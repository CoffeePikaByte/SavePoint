using GameHub.Application.Exceptions;
using GameHub.Application.Interfaces.Repositories;
using GameHub.Application.UserCases;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;

namespace GameHub.Application.Tests;

public class GetUserLibraryUseCaseTests
{
    private readonly Users _users = new();
    private readonly Library _library = new();

    [Fact]
    public async Task ExecuteAsync_MapsLoadedGamesAndPreservesRepositoryOrder()
    {
        var game = new Game
        {
            Id = Guid.NewGuid(), Title = "Celeste", CoverUrl = "https://example.test/celeste.png",
            ReleaseDate = new DateTime(2018, 1, 25)
        };
        _library.Result.Add(new UserGame
        {
            Id = Guid.NewGuid(), UserId = _users.Id, GameId = game.Id, Game = game,
            Status = GameStatus.Playing, HoursPlayed = 12, AddedAt = DateTime.UtcNow
        });
        _library.Result.Add(new UserGame
        {
            Id = Guid.NewGuid(), UserId = _users.Id, GameId = Guid.NewGuid(),
            Game = new Game { Title = "Portal" }, AddedAt = DateTime.UtcNow.AddDays(-1)
        });

        var result = await new GetUserLibraryUseCase(_users, _library).ExecuteAsync(_users.Id);

        Assert.Equal(_library.Result.Select(item => item.Id), result.Select(item => item.Id));
        for (var index = 0; index < result.Count; index++)
        {
            var expected = _library.Result[index];
            var actual = result[index];
            Assert.Equal(expected.GameId, actual.GameId);
            Assert.Equal(expected.Game.Title, actual.Title);
            Assert.Equal(expected.Game.CoverUrl, actual.CoverUrl);
            Assert.Equal(expected.Game.ReleaseDate, actual.ReleaseDate);
            Assert.Equal(expected.Status, actual.Status);
            Assert.Equal(expected.HoursPlayed, actual.HoursPlayed);
            Assert.Equal(expected.AddedAt, actual.AddedAt);
        }
        Assert.Equal(_users.Id, _users.RequestedId);
        Assert.Equal(_users.Id, _library.RequestedId);
        Assert.Equal(1, _library.Reads);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingUserWithoutEntries_ReturnsEmptyList()
    {
        var result = await new GetUserLibraryUseCase(_users, _library).ExecuteAsync(_users.Id);
        Assert.NotNull(result);
        Assert.Empty(result);
        Assert.Equal(_users.Id, _library.RequestedId);
        Assert.Equal(1, _library.Reads);
    }

    [Fact]
    public async Task ExecuteAsync_DeletedUser_DoesNotQueryLibrary()
    {
        var error = await Assert.ThrowsAsync<UserNotFoundException>(() =>
            new GetUserLibraryUseCase(_users, _library).ExecuteAsync(Guid.NewGuid()));
        Assert.Equal("Usuario no encontrado.", error.Message);
        Assert.Equal(0, _library.Reads);
    }

    private sealed class Users : IUserRepository
    {
        public Guid Id { get; } = Guid.NewGuid();
        public Guid? RequestedId { get; private set; }
        public Task<User?> GetByIdAsync(Guid id)
        {
            RequestedId = id;
            return Task.FromResult(id == Id ? new User { Id = id } : null);
        }
        public Task<User?> GetByEmailAsync(string email) => throw new NotSupportedException();
        public Task AddAsync(User user) => throw new NotSupportedException();
    }

    private sealed class Library : IUserGameRepository
    {
        public List<UserGame> Result { get; } = new();
        public Guid? RequestedId { get; private set; }
        public int Reads { get; private set; }
        public Task<List<UserGame>> GetByUserIdAsync(Guid userId)
        {
            Reads++;
            RequestedId = userId;
            return Task.FromResult(Result);
        }
        // All writes and unrelated reads are forbidden by this use case's test double.
        public Task AddAsync(UserGame item) => throw new NotSupportedException();
        public Task<UserGame?> GetAsync(Guid userId, Guid gameId) => throw new NotSupportedException();
        public Task UpdateAsync(UserGame item) => throw new NotSupportedException();
        public Task DeleteAsync(UserGame item) => throw new NotSupportedException();
    }
}
