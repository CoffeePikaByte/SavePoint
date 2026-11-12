using GameHub.Application.Exceptions;
using GameHub.Application.Interfaces.Repositories;
using GameHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameHub.Infrastructure.Persistence.Repositories;

public class UserGameRepository(GameHubDbContext context) : IUserGameRepository
{
    public async Task AddAsync(UserGame item)
    {
        context.UserGames.Add(item);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_UserGames_UserId_GameId"
        })
        {
            throw new GameAlreadyInLibraryException(ex);
        }
    }

    public Task<UserGame?> GetAsync(Guid userId, Guid gameId) =>
        context.UserGames.SingleOrDefaultAsync(item => item.UserId == userId && item.GameId == gameId);

    public Task<List<UserGame>> GetByUserIdAsync(Guid userId) =>
        context.UserGames
            .Where(item => item.UserId == userId)
            .AsNoTracking()
            .Include(item => item.Game)
            .OrderByDescending(item => item.AddedAt)
            .ThenBy(item => item.Id)
            .ToListAsync();

    public async Task UpdateAsync(UserGame item)
    {
        var entry = context.Entry(item);
        if (entry.State is not (EntityState.Unchanged or EntityState.Modified))
            throw new InvalidOperationException("Update requires an existing entry tracked by this context.");

        try
        {
            // Preserve original values: only detected changes, not the entire row, are written.
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex) when (ex.Entries.Count == 1 && ReferenceEquals(ex.Entries[0].Entity, item))
        {
            // A zero-row write is not proof of deletion. Confirm it against the database.
            // Failed verification and non-deletion concurrency failures remain server errors.
            if (await ex.Entries[0].GetDatabaseValuesAsync() is null)
                throw new LibraryGameNotFoundException(ex);
            throw;
        }
    }

    public async Task DeleteAsync(UserGame item)
    {
        context.UserGames.Remove(item);
        await context.SaveChangesAsync();
    }
}
