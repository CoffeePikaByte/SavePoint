using GameHub.Domain.Enums;

namespace GameHub.Domain.Entities;

public class UserGame
{
    public Guid Id {get;set;}
    public Guid UserId {get;set;}
    public User User {get;set;} = null!;
    public Guid GameId {get;set;}
    public Game Game {get;set;} = null!;
    public GameStatus Status {get;set;}
    public int? HoursPlayed {get;set;}
    public DateTime AddedAt {get;set;} = DateTime.UtcNow;

}