
namespace GameHub.Application.DTOs.Games;

public class ExternalGameDTO
{
    public int ExternalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CoverUrl { get; set; }
    public DateTime? ReleaseDate { get; set; }
}
