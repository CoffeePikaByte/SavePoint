using GameHub.Application.Exceptions;
using GameHub.Domain.Entities;
using GameHub.Infrastructure.Persistence;
using GameHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameHub.API.Tests;

// Synthetic provider exceptions verify translation only, not PostgreSQL or concurrency behavior.
public class UserGameRepositoryTests
{
    [Theory]
    [InlineData("23505", "IX_UserGames_UserId_GameId", true)]
    [InlineData("23505", "PK_UserGames", false)]
    [InlineData("23503", "IX_UserGames_UserId_GameId", false)]
    public async Task AddAsync_TranslatesOnlyTheLibraryUniqueViolation(string sqlState, string constraint, bool duplicate)
    {
        var providerError = new PostgresException("synthetic failure", "ERROR", "ERROR", sqlState,
            constraintName: constraint);
        var error = new DbUpdateException("synthetic save failure", providerError);
        await using var context = new FailingContext(error);
        var repository = new UserGameRepository(context);
        var item = new UserGame { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), GameId = Guid.NewGuid() };

        if (duplicate)
        {
            var translated = await Assert.ThrowsAsync<GameAlreadyInLibraryException>(() => repository.AddAsync(item));
            Assert.Same(error, translated.InnerException);
        }
        else
        {
            Assert.Same(error, await Assert.ThrowsAsync<DbUpdateException>(() => repository.AddAsync(item)));
        }
    }

    [Fact]
    public async Task AddAsync_NonProviderFailure_IsNotTranslated()
    {
        var error = new DbUpdateException("synthetic failure", new InvalidOperationException());
        await using var context = new FailingContext(error);
        var repository = new UserGameRepository(context);
        Assert.Same(error, await Assert.ThrowsAsync<DbUpdateException>(() => repository.AddAsync(new UserGame())));
    }

    private sealed class FailingContext(Exception error) : GameHubDbContext(
        new DbContextOptionsBuilder<GameHubDbContext>()
            .UseNpgsql("Host=unused.invalid;Database=unused;Username=unused;Password=unused")
            .Options)
    {
        // No connection is opened: the save operation is replaced with a deterministic failure.
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<int>(error);
    }
}
