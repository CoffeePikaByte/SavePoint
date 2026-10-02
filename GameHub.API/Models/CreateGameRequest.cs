
namespace GameHub.Application.DTOs.Games;

public class CreateGameRequest 
{
    public int ExternalId {get;set;}
    public string Title {get;set;}
    public string? CoverUrl {get;set;}
    public DateTime? ReleaseDate {get;set;}
}