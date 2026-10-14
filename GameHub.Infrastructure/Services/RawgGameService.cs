using GameHub.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using GameHub.Application.DTOs.Games;
using System.Text.Json;


namespace GameHub.Infrastructure.Services;

public class RawgGameService : IExternalGameService
{

    private readonly HttpClient _httpClient;
    private readonly  string _apiKey;


    public RawgGameService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Rawg:ApiKey"];
    }

    public async Task<ExternalGameDTO?> GetGameAsync(int externalId)
    {
        var response = await _httpClient.GetAsync($"games/{externalId}?key={_apiKey}");

        response.EnsureSuccessStatusCode();
        
        var responseString = await response.Content.ReadAsStringAsync();

        var rawgGame = JsonSerializer.Deserialize<RawgGameResponse>(responseString);

        if(rawgGame == null)
        {
            return null;
        }

        var gameDTO = new ExternalGameDTO
        {
          ExternalId = rawgGame.Id,
          Title = rawgGame.Name ?? string.Empty,
          CoverUrl = rawgGame.BackgroundImage,
          ReleaseDate = rawgGame.Released  
        };

        return gameDTO;
    }

}