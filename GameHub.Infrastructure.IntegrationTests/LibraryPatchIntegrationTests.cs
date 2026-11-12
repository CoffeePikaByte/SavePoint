using GameHub.Application.DTOs.Games;
using GameHub.Application.Exceptions;
using GameHub.Application.UserCases;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameHub.Infrastructure.IntegrationTests;

public class LibraryPatchIntegrationTests
{
    private static async Task<UserGame> Seed(TestDatabase database, bool otherUser = false)
    {
        var item = database.NewEntry();
        if (otherUser) item.UserId = database.OtherUserId;
        item.Status = GameStatus.Playing;
        item.HoursPlayed = 7;
        await using var context = database.CreateContext();
        await new UserGameRepository(context).AddAsync(item);
        return item;
    }

    [Theory]
    [InlineData(true, 2, false, null)]
    [InlineData(false, null, true, 0)]
    [InlineData(false, null, true, null)]
    [InlineData(true, 3, true, 12)]
    [InlineData(true, 1, true, 7)]
    public async Task ExecuteAsync_PersistsOnlyProvidedFieldsAndPreservesOtherOwner(bool hasStatus, int? status, bool hasHours, int? hours)
    {
        await using var database = await TestDatabase.CreateAsync();
        var item = await Seed(database);
        var other = await Seed(database, true);
        await using (var context = database.CreateContext())
        {
            var result = await new UpdateLibraryGameUseCase(new UserRepository(context), new UserGameRepository(context))
                .ExecuteAsync(database.UserId, database.GameId, new UpdateLibraryGameCommand(hasStatus, (GameStatus?)status, hasHours, hours));
            Assert.Equal(item.Id, result.Id);
            Assert.Equal(item.GameId, result.GameId);
            Assert.Equal(hasStatus ? (GameStatus)status!.Value : GameStatus.Playing, result.Status);
            Assert.Equal(hasHours ? hours : 7, result.HoursPlayed);
            Assert.Equal(item.AddedAt, result.AddedAt);
        }
        await using var verifier = database.CreateContext();
        var actual = await verifier.UserGames.SingleAsync(entry => entry.Id == item.Id);
        Assert.Equal((item.Id, item.UserId, item.GameId, item.AddedAt), (actual.Id, actual.UserId, actual.GameId, actual.AddedAt));
        Assert.Equal(hasStatus ? (GameStatus)status!.Value : GameStatus.Playing, actual.Status);
        Assert.Equal(hasHours ? hours : 7, actual.HoursPlayed);
        var untouched = await verifier.UserGames.SingleAsync(entry => entry.Id == other.Id);
        Assert.Equal(GameStatus.Playing, untouched.Status);
        Assert.Equal(7, untouched.HoursPlayed);
        Assert.Equal(2, await verifier.UserGames.CountAsync());
        Assert.Equal(1, await verifier.Games.CountAsync());
    }

    [Theory]
    [InlineData(false, null, false, null)]
    [InlineData(true, null, false, null)]
    [InlineData(true, 4, false, null)]
    [InlineData(true, -1, false, null)]
    [InlineData(true, 2, true, -1)]
    public async Task ExecuteAsync_InvalidCommand_DoesNotMutateEvenAnAlreadyTrackedEntry(bool hasStatus, int? status, bool hasHours, int? hours)
    {
        await using var database = await TestDatabase.CreateAsync();
        var item = await Seed(database);
        await using (var context = database.CreateContext())
        {
            var tracked = await new UserGameRepository(context).GetAsync(item.UserId, item.GameId);
            Assert.NotNull(tracked);
            await Assert.ThrowsAsync<InvalidLibraryGameUpdateException>(() =>
                new UpdateLibraryGameUseCase(new UserRepository(context), new UserGameRepository(context)).ExecuteAsync(
                    item.UserId, item.GameId, new UpdateLibraryGameCommand(hasStatus, (GameStatus?)status, hasHours, hours)));
            Assert.Equal(GameStatus.Playing, tracked.Status);
            Assert.Equal(7, tracked.HoursPlayed);
            Assert.Equal(0, await context.SaveChangesAsync());
        }
        await using var verifier = database.CreateContext();
        var actual = await verifier.UserGames.SingleAsync();
        Assert.Equal(GameStatus.Playing, actual.Status);
        Assert.Equal(7, actual.HoursPlayed);
        Assert.Equal(item.AddedAt, actual.AddedAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_OtherOwnersEntryOrDeletedUser_CannotBeUpdated(bool deleted)
    {
        await using var database = await TestDatabase.CreateAsync();
        var item = await Seed(database, !deleted);
        if (deleted)
        {
            await using var deletion = database.CreateContext();
            deletion.Users.Remove(await deletion.Users.SingleAsync(user => user.Id == database.UserId));
            await deletion.SaveChangesAsync();
        }
        await using var context = database.CreateContext();
        var useCase = new UpdateLibraryGameUseCase(new UserRepository(context), new UserGameRepository(context));
        var command = new UpdateLibraryGameCommand(true, GameStatus.Complete, false, null);
        if (deleted)
            await Assert.ThrowsAsync<UserNotFoundException>(() => useCase.ExecuteAsync(database.UserId, database.GameId, command));
        else
            await Assert.ThrowsAsync<LibraryGameNotFoundException>(() => useCase.ExecuteAsync(database.UserId, database.GameId, command));
        Assert.Equal(0, await context.SaveChangesAsync());
        await using var verifier = database.CreateContext();
        if (deleted) Assert.Empty(await verifier.UserGames.ToListAsync());
        else Assert.Equal(GameStatus.Playing, (await verifier.UserGames.SingleAsync(entry => entry.Id == item.Id)).Status);
    }

    [Fact]
    public async Task UpdateAsync_ConcurrentHoursCommit_IsNotOverwrittenByStatusOnlyUpdate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var item = await Seed(database);
        var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var hoursCommitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<int> UpdateStatus()
        {
            await using var context = database.CreateContext();
            await context.Database.OpenConnectionAsync();
            var pid = ((NpgsqlConnection)context.Database.GetDbConnection()).ProcessID;
            var repository = new UserGameRepository(context);
            var entry = await repository.GetAsync(item.UserId, item.GameId);
            Assert.NotNull(entry);
            loaded.TrySetResult();
            await hoursCommitted.Task.WaitAsync(TimeSpan.FromSeconds(15));
            entry.Status = GameStatus.Complete;
            await repository.UpdateAsync(entry);
            return pid;
        }

        async Task<int> UpdateHours()
        {
            try
            {
                await loaded.Task.WaitAsync(TimeSpan.FromSeconds(15));
                await using var context = database.CreateContext();
                await context.Database.OpenConnectionAsync();
                var pid = ((NpgsqlConnection)context.Database.GetDbConnection()).ProcessID;
                var repository = new UserGameRepository(context);
                var entry = await repository.GetAsync(item.UserId, item.GameId);
                Assert.NotNull(entry);
                entry.HoursPlayed = 19;
                await repository.UpdateAsync(entry);
                return pid;
            }
            finally { hoursCommitted.TrySetResult(); }
        }

        var pids = await ConcurrencyWait.DrainAsync(Task.WhenAll(UpdateStatus(), UpdateHours()), TimeSpan.FromSeconds(30));
        Assert.NotEqual(pids[0], pids[1]);
        await using var verifier = database.CreateContext();
        var actual = await verifier.UserGames.SingleAsync();
        Assert.Equal(GameStatus.Complete, actual.Status);
        Assert.Equal(19, actual.HoursPlayed);
        Assert.Equal((item.Id, item.UserId, item.GameId, item.AddedAt), (actual.Id, actual.UserId, actual.GameId, actual.AddedAt));
    }

    [Fact]
    public async Task UpdateAsync_DetachedEntry_IsExplicitlyRejectedWithoutWriting()
    {
        await using var database = await TestDatabase.CreateAsync();
        await Seed(database);
        await using (var context = database.CreateContext())
        {
            var item = Assert.Single(await new UserGameRepository(context).GetByUserIdAsync(database.UserId));
            item.Status = GameStatus.Complete;
            await Assert.ThrowsAsync<InvalidOperationException>(() => new UserGameRepository(context).UpdateAsync(item));
            Assert.Equal(0, await context.SaveChangesAsync());
        }
        await using var verifier = database.CreateContext();
        Assert.Equal(GameStatus.Playing, (await verifier.UserGames.SingleAsync()).Status);
    }

    [Fact]
    public async Task UpdateAsync_ConfirmedConcurrentDeletion_ReportsLibraryEntryNotFound()
    {
        await using var database = await TestDatabase.CreateAsync();
        var item = await Seed(database);
        await using var original = database.CreateContext();
        var entry = await new UserGameRepository(original).GetAsync(item.UserId, item.GameId);
        Assert.NotNull(entry);
        await using (var deletion = database.CreateContext())
        {
            deletion.UserGames.Remove(await deletion.UserGames.SingleAsync());
            await deletion.SaveChangesAsync();
        }
        entry.Status = GameStatus.Complete;
        await Assert.ThrowsAsync<LibraryGameNotFoundException>(() => new UserGameRepository(original).UpdateAsync(entry));
        await using var verifier = database.CreateContext();
        Assert.Empty(await verifier.UserGames.ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_ZeroAffectedRowsButEntryStillExists_DoesNotBecomeNotFound()
    {
        await using var database = await TestDatabase.CreateAsync();
        await Seed(database);
        await using var context = database.CreateContext();
        // Test-database-only trigger produces a genuine zero-row update without deleting the row.
        // This exercises the safety check rather than fabricating a provider exception.
        await context.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION suppress_library_update() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RETURN NULL; END $$;
            CREATE TRIGGER suppress_library_update BEFORE UPDATE ON "UserGames"
            FOR EACH ROW EXECUTE FUNCTION suppress_library_update();
            """);
        var repository = new UserGameRepository(context);
        var item = await repository.GetAsync(database.UserId, database.GameId);
        Assert.NotNull(item);
        item.Status = GameStatus.Complete;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repository.UpdateAsync(item));
        await using var verifier = database.CreateContext();
        Assert.Equal(GameStatus.Playing, (await verifier.UserGames.SingleAsync()).Status);
    }
}
