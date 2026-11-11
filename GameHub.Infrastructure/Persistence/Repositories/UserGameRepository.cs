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
        context.UserGames.Update(item);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(UserGame item)
    {
        context.UserGames.Remove(item);
        await context.SaveChangesAsync();
    }
}
