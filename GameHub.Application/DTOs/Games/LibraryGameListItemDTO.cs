using GameHub.Domain.Enums;

namespace GameHub.Application.DTOs.Games;

public record LibraryGameListItemDTO(
    Guid Id, Guid GameId, string Title, string? CoverUrl, DateTime? ReleaseDate,
    GameStatus Status, int? HoursPlayed, DateTime AddedAt);
