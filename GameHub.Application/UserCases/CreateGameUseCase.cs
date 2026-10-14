using GameHub.Application.Interfaces.Repositories;
using GameHub.Application.Interfaces.Services;
using GameHub.Domain.Entities;

namespace GameHub.Application.UserCases;

public class CreateGameUseCase
{
    private readonly IGameRepository _gameRepository; 
    private readonly IExternalGameService _externalGameService;

    public CreateGameUseCase(IGameRepository gameRepository, IExternalGameService externalGameService)
    {
        _externalGameService = externalGameService;
        _gameRepository = gameRepository;
    }

    public async Task<Game> ExecuteAsync(int externalId)
    {

        var existingGame = await _gameRepository.GetByExternalIdAsync(externalId);

        if(existingGame is not null)
        {
            throw new Exception("VideoJuego ya guardado.");
        }

        var externalGame = await _externalGameService.GetGameAsync(externalId);

        if(externalGame == null)
        {
            throw new Exception("El videojuego no fue encontrado");
        }

        var game = new Game
        {
            Id = Guid.NewGuid(),
            ExternalId = externalGame.ExternalId,
            Title = externalGame.Title,
            CoverUrl = externalGame.CoverUrl,
            ReleaseDate = externalGame.ReleaseDate,
        };

        await _gameRepository.AddAsync(game);
        
        return game;
    }
    
}
