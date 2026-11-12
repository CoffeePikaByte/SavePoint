using GameHub.Application.DTOs.Games;
using GameHub.Application.Interfaces.Repositories;
using GameHub.Application.Exceptions;

namespace GameHub.Application.UserCases;

public class UpdateLibraryGameUseCase(IUserRepository users, IUserGameRepository library)
{
    public async Task<LibraryGameDTO> ExecuteAsync(Guid userId, Guid gameId, UpdateLibraryGameCommand command)
    {
        // Validate the complete command before querying or mutating a tracked entry.
        if (gameId == Guid.Empty)
            throw new InvalidLibraryGameUpdateException("A non-empty gameId is required.");
        if (!command.HasStatus && !command.HasHoursPlayed)
            throw new InvalidLibraryGameUpdateException("At least one update field is required.");
        if (command.HasStatus && (command.Status is null || !Enum.IsDefined(command.Status.Value)))
            throw new InvalidLibraryGameUpdateException("Status must be a defined, non-null game status.");
        if (command.HasHoursPlayed && command.HoursPlayed is < 0)
            throw new InvalidLibraryGameUpdateException("Hours played cannot be negative.");

        if (await users.GetByIdAsync(userId) is null)
            throw new UserNotFoundException("Usuario no encontrado.");
        var item = await library.GetAsync(userId, gameId) ?? throw new LibraryGameNotFoundException();

        if (command.HasStatus) item.Status = command.Status!.Value;
        if (command.HasHoursPlayed) item.HoursPlayed = command.HoursPlayed;
        await library.UpdateAsync(item);
        return new LibraryGameDTO(item.Id, item.GameId, item.Status, item.HoursPlayed, item.AddedAt);
    }
}
