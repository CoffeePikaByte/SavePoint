using GameHub.Domain.Entities;

namespace GameHub.Application.Interfaces.Repositories;

public interface IUserGameRepository
{
       Task AddAsync(UserGame userGame);
       Task<List<UserGame>> GetByUserIdAsync(Guid userId);
       Task<UserGame?> GetAsync(Guid userId, Guid gameId);
       Task UpdateAsync(UserGame userGame);
       Task DeleteAsync(UserGame userGame);
}