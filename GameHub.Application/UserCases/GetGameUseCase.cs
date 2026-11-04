
using GameHub.Application.Interfaces.Repositories;
using GameHub.Domain.Entities;

namespace GameHub.Application.UserCases;

public class GetGameUseCase
{
    private readonly IGameRepository _gameRepository;

    public GetGameUseCase(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }    

    public async Task<List<Game>> ExecuteAsync()
    {
        return await _gameRepository.GetAllGamesAsync();
    }

}
