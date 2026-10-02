using GameHub.Application.Interfaces.Repositories;
using GameHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Infrastructure.Persistence.Repositories;


public class GameRepository : IGameRepository
{
    private readonly GameHubDbContext _dbContext; 

    public GameRepository(GameHubDbContext dbContext)
    {   
        _dbContext = dbContext;
    }

    public async Task<Game?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Games.FindAsync(id);
    }

    public async Task<Game?> GetByExternalIdAsync(int externalId)
    {
        return await _dbContext.Games.FirstOrDefaultAsync(game => game.ExternalId == externalId);
    }

    public async Task AddAsync(Game game)
    {
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();
    }


}