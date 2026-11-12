using GameHub.Domain.Entities;

namespace GameHub.Application.Interfaces.Repositories;

public interface IUserGameRepository
{
       Task AddAsync(UserGame userGame);
       Task<List<UserGame>> GetByUserIdAsync(Guid userId);
       Task<UserGame?> GetAsync(Guid userId, Guid gameId);
       /// <summary>
       /// Saves detected changes to an existing entry loaded by GetAsync in the same context.
       /// Detached, added or deleted entries are rejected rather than attached or updated wholesale.
       /// </summary>
       Task UpdateAsync(UserGame userGame);
       Task DeleteAsync(UserGame userGame);
}