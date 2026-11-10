using GameHub.Application.Exceptions;
using GameHub.Application.Interfaces.Repositories;
using GameHub.Application.UserCases;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;

namespace GameHub.Application.Tests;

public class AddGameToLibraryUseCaseTests
{
    private readonly Users _users = new();
    private readonly Games _games = new();
    private readonly Library _library = new();

    private AddGameToLibraryUseCase Create() => new(_users, _games, _library);

    [Fact]
    public async Task ExecuteAsync_AddsLocalGameWithInitialValues()
    {
        var before = DateTime.UtcNow;
        var result = await Create().ExecuteAsync(_users.Id, _games.Id);

        var stored = Assert.Single(_library.Items);
        Assert.NotEqual(Guid.Empty, stored.Id);
        Assert.Equal(_users.Id, stored.UserId);
        Assert.Equal(_games.Id, stored.GameId);
        Assert.Equal(GameStatus.BackLog, stored.Status);
        Assert.Null(stored.HoursPlayed);
        Assert.Equal(DateTimeKind.Utc, stored.AddedAt.Kind);
        Assert.InRange(stored.AddedAt, before, DateTime.UtcNow);
        Assert.Equal(stored.Id, result.Id);
        Assert.Equal(stored.GameId, result.GameId);
        Assert.Equal(stored.Status, result.Status);
        Assert.Equal(stored.HoursPlayed, result.HoursPlayed);
        Assert.Equal(stored.AddedAt, result.AddedAt);
    }

    [Fact]
    public async Task ExecuteAsync_MissingUser_DoesNotWrite()
    {
        await Assert.ThrowsAsync<UserNotFoundException>(() => Create().ExecuteAsync(Guid.NewGuid(), _games.Id));
        Assert.Empty(_library.Items);
        Assert.Equal(0, _games.Reads);
    }

    [Fact]
    public async Task ExecuteAsync_MissingGame_DoesNotWrite()
    {
        await Assert.ThrowsAsync<GameNotFoundException>(() => Create().ExecuteAsync(_users.Id, Guid.NewGuid()));
        Assert.Empty(_library.Items);
    }

    [Fact]
    public async Task ExecuteAsync_Duplicate_DoesNotWriteAgain()
    {
        var useCase = Create();
        await useCase.ExecuteAsync(_users.Id, _games.Id);
        await Assert.ThrowsAsync<GameAlreadyInLibraryException>(() => useCase.ExecuteAsync(_users.Id, _games.Id));
        Assert.Single(_library.Items);
    }

    [Fact]
    public async Task ExecuteAsync_AnotherUsersEntry_DoesNotPreventAddition()
    {
        _library.Items.Add(new UserGame { UserId = Guid.NewGuid(), GameId = _games.Id });
        await Create().ExecuteAsync(_users.Id, _games.Id);
        Assert.Equal(2, _library.Items.Count);
        Assert.Single(_library.Items, item => item.UserId == _users.Id);
    }

    [Fact]
    public async Task ExecuteAsync_SaveConflict_PropagatesWithoutReturningSuccess()
    {
        _library.SaveError = new GameAlreadyInLibraryException();
        await Assert.ThrowsAsync<GameAlreadyInLibraryException>(() => Create().ExecuteAsync(_users.Id, _games.Id));
        Assert.Empty(_library.Items);
    }

    private sealed class Users : IUserRepository
    {
        public Guid Id { get; } = Guid.NewGuid();
        public Task<User?> GetByIdAsync(Guid id) => Task.FromResult(id == Id ? new User { Id = Id } : null);
        public Task<User?> GetByEmailAsync(string email) => throw new NotSupportedException();
        public Task AddAsync(User user) => throw new NotSupportedException();
    }

    private sealed class Games : IGameRepository
    {
        public Guid Id { get; } = Guid.NewGuid();
        public int Reads { get; private set; }
        public Task<Game?> GetByIdAsync(Guid id)
        {
            Reads++;
            return Task.FromResult(id == Id ? new Game { Id = Id } : null);
        }
        public Task<Game?> GetByExternalIdAsync(int id) => throw new NotSupportedException();
        public Task<List<Game>> GetAllGamesAsync() => throw new NotSupportedException();
        public Task AddAsync(Game game) => throw new NotSupportedException();
    }

    private sealed class Library : IUserGameRepository
    {
        public List<UserGame> Items { get; } = new();
        public Exception? SaveError { get; set; }
        public Task<UserGame?> GetAsync(Guid userId, Guid gameId) =>
            Task.FromResult(Items.SingleOrDefault(item => item.UserId == userId && item.GameId == gameId));
        public Task AddAsync(UserGame item)
        {
            if (SaveError is not null) throw SaveError;
            Items.Add(item);
            return Task.CompletedTask;
        }
        public Task<List<UserGame>> GetByUserIdAsync(Guid userId) => throw new NotSupportedException();
        public Task UpdateAsync(UserGame item) => throw new NotSupportedException();
        public Task DeleteAsync(UserGame item) => throw new NotSupportedException();
    }
}
