using GameHub.Domain.Entities;

namespace GameHub.Application.Interfaces.Repositories;

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(Guid id);
    Task<Game?> GetByExternalIdAsync(int externalId);

    Task AddAsync(Game game);

}