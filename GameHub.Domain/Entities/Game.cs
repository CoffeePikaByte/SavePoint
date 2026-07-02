namespace GameHub.Domain.Entities;

public class Game
{
    public Guid Id {get;set;}
    public int ExternalId {get;set;}
    public string Title {get;set;}
    public string? CoverUrl {get;set;}
    public DateTime? ReleaseDate {get;set;}

}