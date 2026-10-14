
using System.Text.Json.Serialization;

namespace GameHub.Infrastructure.Services;


public class RawgGameResponse
{
    [JsonPropertyName("id")]
    public int Id {get; set;}

    [JsonPropertyName("name")]
    public string? Name {get; set;}

    [JsonPropertyName("released")]
    public DateTime? Released {get;  set;}

    [JsonPropertyName("background_image")]

    public string? BackgroundImage {get; set;}



}