using GameHub.Application.DTOs.Games;
using GameHub.Application.Exceptions;
using GameHub.Application.Interfaces.Repositories;

namespace GameHub.Application.UserCases;

public class GetUserLibraryUseCase(IUserRepository users, IUserGameRepository library)
{
    public async Task<List<LibraryGameListItemDTO>> ExecuteAsync(Guid userId)
    {
        if (await users.GetByIdAsync(userId) is null)
            throw new UserNotFoundException("Usuario no encontrado.");

        var entries = await library.GetByUserIdAsync(userId);
        return entries.Select(item => new LibraryGameListItemDTO(
            item.Id, item.GameId, item.Game.Title, item.Game.CoverUrl, item.Game.ReleaseDate,
            item.Status, item.HoursPlayed, item.AddedAt)).ToList();
    }
}
