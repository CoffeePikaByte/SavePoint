using GameHub.Application.Interfaces.Repositories;
using GameHub.Domain.Entities;

namespace GameHub.Application.UserCases;

public class CreateGameUseCase
{
    private readonly IGameRepository _gameRepository; 

    public CreateGameUseCase(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Game> ExecuteAsync(int externalId, string title, string coverUrl, DateTime releaseDate)
    {
        var existingGame = await _gameRepository.GetByExternalIdAsync(externalId);

        if(existingGame is not null)
        {
            throw new Exception("Ocurrio un error inesperado.");
        }

        var game = new Game
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            Title = title,
            CoverUrl = coverUrl,
            ReleaseDate = releaseDate,
        };

        await _gameRepository.AddAsync(game);
        
        return game;
    }
    
}
