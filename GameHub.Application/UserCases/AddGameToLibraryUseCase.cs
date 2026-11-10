using GameHub.Application.DTOs.Games;
using GameHub.Application.Exceptions;
using GameHub.Application.Interfaces.Repositories;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;

namespace GameHub.Application.UserCases;

public class AddGameToLibraryUseCase(
    IUserRepository users, IGameRepository games, IUserGameRepository library)
{
    public async Task<LibraryGameDTO> ExecuteAsync(Guid userId, Guid gameId)
    {
        if (await users.GetByIdAsync(userId) is null)
            throw new UserNotFoundException("Usuario no encontrado.");

        if (await games.GetByIdAsync(gameId) is null)
            throw new GameNotFoundException();

        if (await library.GetAsync(userId, gameId) is not null)
            throw new GameAlreadyInLibraryException();

        var item = new UserGame
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GameId = gameId,
            Status = GameStatus.BackLog,
            HoursPlayed = null,
            AddedAt = DateTime.UtcNow
        };
        await library.AddAsync(item);
        return new LibraryGameDTO(item.Id, item.GameId, item.Status, item.HoursPlayed, item.AddedAt);
    }
}
