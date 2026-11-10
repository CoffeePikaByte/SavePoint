using GameHub.Domain.Enums;

namespace GameHub.Application.DTOs.Games;

public record LibraryGameDTO(Guid Id, Guid GameId, GameStatus Status, int? HoursPlayed, DateTime AddedAt);
