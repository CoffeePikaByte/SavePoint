using GameHub.Domain.Entities;

namespace GameHub.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);

    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
}