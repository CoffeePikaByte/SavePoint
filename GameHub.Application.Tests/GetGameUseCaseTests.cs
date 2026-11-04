using GameHub.Application.Interfaces.Repositories;
using GameHub.Application.UserCases;
using GameHub.Domain.Entities;

namespace GameHub.Application.Tests;

public class GetGameUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCatalogHasGames_ReturnsGames()
    {
        // Arrange
        var games = new List<Game>
        {
            new() { Id = Guid.NewGuid(), ExternalId = 1, Title = "Portal" },
            new() { Id = Guid.NewGuid(), ExternalId = 2, Title = "Celeste" }
        };
        var repository = new InMemoryGameRepository(games);
        var useCase = new GetGameUseCase(repository);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert: compare contents without requiring a particular order.
        Assert.Equal(games.Count, result.Count);
        foreach (var game in games)
        {
            Assert.Contains(game, result);
        }
        Assert.Equal(1, repository.GetAllCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCatalogIsEmpty_ReturnsEmptyList()
    {
        // Arrange
        var repository = new InMemoryGameRepository(new List<Game>());
        var useCase = new GetGameUseCase(repository);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        Assert.Equal(1, repository.GetAllCalls);
    }

    private sealed class InMemoryGameRepository : IGameRepository
    {
        private readonly List<Game> _games;

        public int GetAllCalls { get; private set; }

        public InMemoryGameRepository(List<Game> games)
        {
            _games = new List<Game>(games);
        }

        public Task<List<Game>> GetAllGamesAsync()
        {
            GetAllCalls++;
            return Task.FromResult(new List<Game>(_games));
        }

        // These operations are outside the listing use case's contract.
        public Task<Game?> GetByIdAsync(Guid id) => throw new NotSupportedException();
        public Task<Game?> GetByExternalIdAsync(int externalId) => throw new NotSupportedException();
        public Task AddAsync(Game game) => throw new NotSupportedException();
    }
}
