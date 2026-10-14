using GameHub.Domain.Entities;
using GameHub.Application.DTOs.Games;

namespace GameHub.Application.Interfaces.Services
{
    public interface IExternalGameService
    {
        Task<ExternalGameDTO?> GetGameAsync(int externalId);

    }

}
