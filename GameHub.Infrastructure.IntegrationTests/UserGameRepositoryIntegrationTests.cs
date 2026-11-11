using GameHub.Application.Exceptions;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameHub.Infrastructure.IntegrationTests;

public class UserGameRepositoryIntegrationTests
{
    [Fact]
    public async Task GetByUserIdAsync_LoadsGameNavigationFromDatabase()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using (var writer = database.CreateContext())
            await new UserGameRepository(writer).AddAsync(database.NewEntry());
        await using var reader = database.CreateContext();
        var item = Assert.Single(await new UserGameRepository(reader).GetByUserIdAsync(database.UserId));
        Assert.NotNull(item.Game);
        Assert.Equal(database.GameId, item.Game.Id);
        Assert.Equal("Integration-only game", item.Game.Title);
    }

    [Fact]
    public async Task GetByUserIdAsync_DoesNotTrackOrPersistChangesToReadResults()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using (var writer = database.CreateContext())
            await new UserGameRepository(writer).AddAsync(database.NewEntry());
        await using (var reader = database.CreateContext())
        {
            var item = Assert.Single(await new UserGameRepository(reader).GetByUserIdAsync(database.UserId));
            Assert.Empty(reader.ChangeTracker.Entries());
            item.HoursPlayed = 999;
            Assert.Equal(0, await reader.SaveChangesAsync());
        }
        await using var verifier = database.CreateContext();
        Assert.Null((await verifier.UserGames.SingleAsync()).HoursPlayed);
    }

    [Fact]
    public async Task GetByUserIdAsync_LoadsGamesInStableOrderWithoutTrackingOrWrites()
    {
        await using var database = await TestDatabase.CreateAsync();
        var time = new DateTime(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);
        var games = new[]
        {
            new Game { Id = Guid.NewGuid(), ExternalId = 41, Title = "Celeste", CoverUrl = "https://example.test/celeste.png", ReleaseDate = new DateTime(2018, 1, 25) },
            new Game { Id = Guid.NewGuid(), ExternalId = 42, Title = "Hades" }
        };
        var older = database.NewEntry();
        older.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        older.AddedAt = time.AddDays(-1);
        var tieLast = database.NewEntry();
        tieLast.Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
        tieLast.GameId = games[1].Id;
        tieLast.Status = GameStatus.Complete;
        tieLast.AddedAt = time;
        var tieFirst = database.NewEntry();
        tieFirst.Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        tieFirst.GameId = games[0].Id;
        tieFirst.Status = GameStatus.Playing;
        tieFirst.HoursPlayed = 12;
        tieFirst.AddedAt = time;
        var other = database.NewEntry();
        other.UserId = database.OtherUserId;
        other.AddedAt = time.AddDays(1);
        Game originalGame;
        await using (var writer = database.CreateContext())
        {
            originalGame = await writer.Games.SingleAsync(game => game.Id == database.GameId);
            writer.Games.AddRange(games);
            await writer.SaveChangesAsync();
            var repository = new UserGameRepository(writer);
            // Deliberately not insertion order or Id order: both ordering keys must be applied.
            foreach (var item in new[] { older, tieLast, other, tieFirst }) await repository.AddAsync(item);
        }

        var expected = new[] { tieFirst, tieLast, older };
        var gameById = games.Append(originalGame).ToDictionary(game => game.Id);
        await using (var reader = database.CreateContext())
        {
            var repository = new UserGameRepository(reader);
            var result = await repository.GetByUserIdAsync(database.UserId);
            Assert.Equal(expected.Select(item => item.Id), result.Select(item => item.Id));
            for (var index = 0; index < result.Count; index++)
            {
                var actual = result[index];
                var entry = expected[index];
                var game = gameById[entry.GameId];
                Assert.Equal(database.UserId, actual.UserId);
                Assert.Equal(entry.GameId, actual.GameId);
                Assert.NotNull(actual.Game);
                Assert.Equal(game.Title, actual.Game.Title);
                Assert.Equal(game.CoverUrl, actual.Game.CoverUrl);
                Assert.Equal(game.ReleaseDate, actual.Game.ReleaseDate);
                Assert.Equal(entry.Status, actual.Status);
                Assert.Equal(entry.HoursPlayed, actual.HoursPlayed);
                Assert.Equal(entry.AddedAt, actual.AddedAt);
            }
            Assert.Equal(expected.Select(item => item.Id), (await repository.GetByUserIdAsync(database.UserId)).Select(item => item.Id));
            Assert.Equal(other.Id, Assert.Single(await repository.GetByUserIdAsync(database.OtherUserId)).Id);
            Assert.Empty(reader.ChangeTracker.Entries());
            result[0].HoursPlayed = 999;
            result[0].Game.Title = "Must not persist from this read";
            Assert.Equal(0, await reader.SaveChangesAsync());
        }
        await using var verifier = database.CreateContext();
        Assert.Equal(4, await verifier.UserGames.CountAsync());
        Assert.Equal(3, await verifier.Games.CountAsync());
        Assert.Equal(12, (await verifier.UserGames.SingleAsync(item => item.Id == tieFirst.Id)).HoursPlayed);
        Assert.Equal(games[0].Title, (await verifier.Games.SingleAsync(game => game.Id == games[0].Id)).Title);
    }

    [Fact]
    public async Task GetByUserIdAsync_EmptyForUserDespiteOtherUsersPersistedEntries()
    {
        await using var database = await TestDatabase.CreateAsync();
        var other = database.NewEntry();
        other.UserId = database.OtherUserId;
        await using (var writer = database.CreateContext())
            await new UserGameRepository(writer).AddAsync(other);
        await using (var reader = database.CreateContext())
        {
            Assert.Empty(await new UserGameRepository(reader).GetByUserIdAsync(database.UserId));
            Assert.Empty(reader.ChangeTracker.Entries());
            Assert.Equal(0, await reader.SaveChangesAsync());
        }
        await using var verifier = database.CreateContext();
        Assert.Equal(other.Id, (await verifier.UserGames.SingleAsync()).Id);
    }

    [Fact]
    public async Task AddAsync_PersistsFields_ReadableFromAnotherContext()
    {
        await using var database = await TestDatabase.CreateAsync();
        var item = database.NewEntry();
        await using (var writer = database.CreateContext())
            await new UserGameRepository(writer).AddAsync(item);

        await using var reader = database.CreateContext();
        var actual = await new UserGameRepository(reader).GetAsync(item.UserId, item.GameId);
        Assert.NotNull(actual);
        Assert.NotSame(item, actual);
        Assert.Equal(item.Id, actual.Id);
        Assert.Equal(item.UserId, actual.UserId);
        Assert.Equal(item.GameId, actual.GameId);
        Assert.Equal(GameStatus.BackLog, actual.Status);
        Assert.Null(actual.HoursPlayed);
        Assert.Equal(item.AddedAt, actual.AddedAt);
        Assert.Equal(DateTimeKind.Utc, actual.AddedAt.Kind);
    }

    [Fact]
    public async Task GetAsync_MissingPair_ReturnsNull()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        Assert.Null(await new UserGameRepository(context).GetAsync(database.UserId, database.GameId));
    }

    [Fact]
    public async Task GetByUserIdAsync_OnlyReturnsThatUsersEntries()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using (var writer = database.CreateContext())
        {
            var repository = new UserGameRepository(writer);
            await repository.AddAsync(database.NewEntry());
            var other = database.NewEntry();
            other.UserId = database.OtherUserId;
            await repository.AddAsync(other);
        }
        await using var reader = database.CreateContext();
        var result = await new UserGameRepository(reader).GetByUserIdAsync(database.UserId);
        Assert.Equal(database.UserId, Assert.Single(result).UserId);
        Assert.Null(await new UserGameRepository(reader).GetAsync(Guid.NewGuid(), database.GameId));
    }

    [Theory]
    [InlineData(true, "FK_UserGames_Users_UserId")]
    [InlineData(false, "FK_UserGames_Games_GameId")]
    public async Task AddAsync_MissingForeignKey_PreservesPostgresError(bool missingUser, string constraint)
    {
        await using var database = await TestDatabase.CreateAsync();
        var item = database.NewEntry();
        if (missingUser) item.UserId = Guid.NewGuid(); else item.GameId = Guid.NewGuid();
        await using (var context = database.CreateContext())
        {
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => new UserGameRepository(context).AddAsync(item));
            AssertProvider(error, PostgresErrorCodes.ForeignKeyViolation, constraint);
        }
        await using var reader = database.CreateContext();
        Assert.Equal(0, await reader.UserGames.CountAsync());
    }

    [Fact]
    public async Task AddAsync_DuplicatePair_TranslatesSpecificUniqueIndex()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using (var first = database.CreateContext())
            await new UserGameRepository(first).AddAsync(database.NewEntry());
        await using (var second = database.CreateContext())
        {
            var error = await Assert.ThrowsAsync<GameAlreadyInLibraryException>(() =>
                new UserGameRepository(second).AddAsync(database.NewEntry()));
            AssertLibraryDuplicate(error);
        }
        await using var reader = database.CreateContext();
        Assert.Equal(1, await reader.UserGames.CountAsync());
    }

    [Fact]
    public async Task AddAsync_DifferentUsers_CanStoreTheSameGame()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using (var first = database.CreateContext())
            await new UserGameRepository(first).AddAsync(database.NewEntry());
        var other = database.NewEntry();
        other.UserId = database.OtherUserId;
        await using (var second = database.CreateContext())
            await new UserGameRepository(second).AddAsync(other);
        await using var reader = database.CreateContext();
        var entries = await reader.UserGames.AsNoTracking().ToListAsync();
        Assert.Equal(2, entries.Count);
        Assert.Equal(2, entries.Select(item => item.UserId).Distinct().Count());
        Assert.All(entries, item => Assert.Equal(database.GameId, item.GameId));
    }

    [Fact]
    public async Task AddAsync_PrimaryKeyConflict_IsNotTranslatedToLibraryDuplicate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var firstItem = database.NewEntry();
        await using (var first = database.CreateContext())
            await new UserGameRepository(first).AddAsync(firstItem);
        var conflicting = database.NewEntry();
        conflicting.Id = firstItem.Id;
        conflicting.UserId = database.OtherUserId; // A different pair avoids violating both unique constraints.
        await using (var second = database.CreateContext())
        {
            var error = await Assert.ThrowsAsync<DbUpdateException>(() =>
                new UserGameRepository(second).AddAsync(conflicting));
            AssertProvider(error, PostgresErrorCodes.UniqueViolation, "PK_UserGames");
        }
        await using var reader = database.CreateContext();
        Assert.Equal(database.UserId, (await reader.UserGames.SingleAsync()).UserId);
    }

    [Fact]
    public async Task AddAsync_ConcurrentSamePair_OneSuccessOneDatabaseConflict()
    {
        await using var database = await TestDatabase.CreateAsync();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = 0;

        async Task<Exception?> Insert()
        {
            await using var context = database.CreateContext();
            var repository = new UserGameRepository(context);
            Assert.Null(await repository.GetAsync(database.UserId, database.GameId));
            if (Interlocked.Increment(ref waiting) == 2) ready.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(15));
            try { await repository.AddAsync(database.NewEntry()); return null; }
            catch (GameAlreadyInLibraryException ex) { return ex; }
            // Timeouts, provider failures and failed assertions fault the task, not a fake conflict result.
        }

        var attempts = Task.WhenAll(Insert(), Insert());
        try
        {
            var firstEvent = await Task.WhenAny(ready.Task, attempts).WaitAsync(TimeSpan.FromSeconds(20));
            if (firstEvent == attempts) await attempts; // Surface a failed worker rather than hide it behind a barrier timeout.
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(20));
        }
        finally
        {
            release.TrySetResult();
            // Drain both workers before the database can be disposed, including failure paths.
            await ConcurrencyWait.DrainAsync(attempts, TimeSpan.FromSeconds(30));
        }
        var outcomes = await attempts;
        Assert.Single(outcomes, outcome => outcome is null);
        AssertLibraryDuplicate(Assert.IsType<GameAlreadyInLibraryException>(Assert.Single(outcomes, outcome => outcome is not null)));
        await using var reader = database.CreateContext();
        var stored = await reader.UserGames.SingleAsync();
        Assert.Equal(database.UserId, stored.UserId);
        Assert.Equal(database.GameId, stored.GameId);
    }

    private static void AssertLibraryDuplicate(GameAlreadyInLibraryException error) =>
        AssertProvider(Assert.IsType<DbUpdateException>(error.InnerException),
            PostgresErrorCodes.UniqueViolation, "IX_UserGames_UserId_GameId");

    private static void AssertProvider(DbUpdateException error, string code, string constraint)
    {
        var postgres = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(code, postgres.SqlState);
        Assert.Equal(constraint, postgres.ConstraintName);
    }
}
