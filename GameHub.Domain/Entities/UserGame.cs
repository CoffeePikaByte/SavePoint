using GameHub.Domain.Enums;

namespace GameHub.Domain.Entities;

public class UserGame
{
    public Guid Id {get;set;}
    public Guid UserId {get;set;}
    public Guid GameId {get;set;}
    public GameStatus Status {get;set;}
    public int? HoursPlayed {get;set;}
    public DateTime AddedAt {get;set;} = DateTime.UtcNow;

}