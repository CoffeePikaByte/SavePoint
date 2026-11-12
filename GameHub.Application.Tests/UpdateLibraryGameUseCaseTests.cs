using GameHub.Application.DTOs.Games;
using GameHub.Application.Exceptions;
using GameHub.Application.Interfaces.Repositories;
using GameHub.Application.UserCases;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;

namespace GameHub.Application.Tests;

public class UpdateLibraryGameUseCaseTests
{
    [Theory]
    [InlineData(true, 2, false, null)]
    [InlineData(false, null, true, 0)]
    [InlineData(false, null, true, null)]
    [InlineData(true, 3, true, 12)]
    [InlineData(true, 1, true, 7)]
    public async Task ExecuteAsync_OnlyChangesProvidedFields(bool hasStatus, int? status, bool hasHours, int? hours)
    {
        var users = new Users();
        var library = new Library(users.Id);
        var item = library.Item;
        var metadata = (item.Id, item.UserId, item.GameId, item.AddedAt);
        var command = new UpdateLibraryGameCommand(hasStatus, (GameStatus?)status, hasHours, hours);

        var result = await new UpdateLibraryGameUseCase(users, library).ExecuteAsync(users.Id, item.GameId, command);

        Assert.Equal(hasStatus ? (GameStatus)status!.Value : GameStatus.Playing, item.Status);
        Assert.Equal(hasHours ? hours : 7, item.HoursPlayed);
        Assert.Equal(metadata, (item.Id, item.UserId, item.GameId, item.AddedAt));
        Assert.Equal(new LibraryGameDTO(item.Id, item.GameId, item.Status, item.HoursPlayed, item.AddedAt), result);
        Assert.Equal((users.Id, item.GameId), library.Requested);
        Assert.Equal(1, library.Updates);
    }

    [Theory]
    [InlineData(false, null, false, null)]
    [InlineData(true, null, false, null)]
    [InlineData(true, -1, false, null)]
    [InlineData(true, 4, false, null)]
    [InlineData(true, 2, true, -1)]
    public async Task ExecuteAsync_InvalidPatch_DoesNotMutateOrReadOrSave(bool hasStatus, int? status, bool hasHours, int? hours)
    {
        var users = new Users();
        var library = new Library(users.Id);
        var before = (library.Item.Status, library.Item.HoursPlayed);
        await Assert.ThrowsAsync<InvalidLibraryGameUpdateException>(() =>
            new UpdateLibraryGameUseCase(users, library).ExecuteAsync(users.Id, library.Item.GameId,
                new UpdateLibraryGameCommand(hasStatus, (GameStatus?)status, hasHours, hours)));
        Assert.Equal(before, (library.Item.Status, library.Item.HoursPlayed));
        Assert.Equal(0, users.Reads);
        Assert.Equal(0, library.Reads);
        Assert.Equal(0, library.Updates);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyGameId_IsInvalidBeforeQueries()
    {
        var users = new Users();
        var library = new Library(users.Id);
        await Assert.ThrowsAsync<InvalidLibraryGameUpdateException>(() =>
            new UpdateLibraryGameUseCase(users, library).ExecuteAsync(users.Id, Guid.Empty,
                new UpdateLibraryGameCommand(true, GameStatus.Complete, false, null)));
        Assert.Equal(0, users.Reads);
        Assert.Equal(0, library.Reads);
        Assert.Equal(0, library.Updates);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_MissingOwnEntryOrDeletedUser_DoesNotSave(bool deletedUser)
    {
        var users = new Users();
        var library = new Library(users.Id);
        var useCase = new UpdateLibraryGameUseCase(users, library);
        var command = new UpdateLibraryGameCommand(true, GameStatus.Complete, false, null);
        if (deletedUser)
            await Assert.ThrowsAsync<UserNotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid(), library.Item.GameId, command));
        else
            await Assert.ThrowsAsync<LibraryGameNotFoundException>(() => useCase.ExecuteAsync(users.Id, Guid.NewGuid(), command));
        Assert.Equal(deletedUser ? 0 : 1, library.Reads);
        Assert.Equal(0, library.Updates);
        Assert.Equal(GameStatus.Playing, library.Item.Status);
    }

    private sealed class Users : IUserRepository
    {
        public Guid Id { get; } = Guid.NewGuid();
        public int Reads { get; private set; }
        public Task<User?> GetByIdAsync(Guid id)
        {
            Reads++;
            return Task.FromResult(id == Id ? new User { Id = Id } : null);
        }
        public Task<User?> GetByEmailAsync(string email) => throw new NotSupportedException();
        public Task AddAsync(User user) => throw new NotSupportedException();
    }

    private sealed class Library(Guid userId) : IUserGameRepository
    {
        public UserGame Item { get; } = new() { Id = Guid.NewGuid(), UserId = userId, GameId = Guid.NewGuid(), Status = GameStatus.Playing, HoursPlayed = 7 };
        public (Guid, Guid)? Requested { get; private set; }
        public int Reads { get; private set; }
        public int Updates { get; private set; }
        public Task<UserGame?> GetAsync(Guid owner, Guid game)
        {
            Reads++;
            Requested = (owner, game);
            return Task.FromResult(owner == Item.UserId && game == Item.GameId ? Item : null);
        }
        public Task UpdateAsync(UserGame item)
        {
            Assert.Same(Item, item);
            Updates++;
            return Task.CompletedTask;
        }
        public Task AddAsync(UserGame item) => throw new NotSupportedException();
        public Task<List<UserGame>> GetByUserIdAsync(Guid owner) => throw new NotSupportedException();
        public Task DeleteAsync(UserGame item) => throw new NotSupportedException();
    }
}
