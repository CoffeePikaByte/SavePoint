using GameHub.Domain.Enums;

namespace GameHub.Application.DTOs.Games;

public record UpdateLibraryGameCommand(bool HasStatus, GameStatus? Status, bool HasHoursPlayed, int? HoursPlayed);
