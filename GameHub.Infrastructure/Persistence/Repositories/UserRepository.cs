using GameHub.Domain.Entities;
using GameHub.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Infrastructure.Persistence.Repositories;


public class UserRepository : IUserRepository
{
    private readonly GameHubDbContext _dbContext;

    public UserRepository(GameHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email);    
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Users
            .FindAsync(id);    
    }
}