using System.Net;
using System.Net.Http.Json;
using GameHub.Application.DTOs.Games;
using GameHub.Domain.Entities;

namespace GameHub.API.Tests;

public class GameControllerTests
{
    [Fact]
    public async Task Get_WithGames_ReturnsOkAndLocalCatalog()
    {
        using var factory = new CatalogApiFactory();
        var games = new[]
        {
            new Game { Id = Guid.NewGuid(), ExternalId = 1, Title = "Portal",
                CoverUrl = "https://example.test/portal.png", ReleaseDate = new DateTime(2007, 10, 10) },
            new Game { Id = Guid.NewGuid(), ExternalId = 2, Title = "Celeste" }
        };
        factory.Repository.Seed(games);
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync("/api/Game");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var result = await response.Content.ReadFromJsonAsync<List<Game>>();
        Assert.NotNull(result);
        Assert.Equal(games.Length, result.Count);
        foreach (var expected in games)
        {
            var actual = Assert.Single(result, game => game.Id == expected.Id);
            Assert.Equal(expected.ExternalId, actual.ExternalId);
            Assert.Equal(expected.Title, actual.Title);
            Assert.Equal(expected.CoverUrl, actual.CoverUrl);
            Assert.Equal(expected.ReleaseDate, actual.ReleaseDate);
        }
        Assert.Equal(1, factory.Repository.ReadAllCalls);
        Assert.Equal(0, factory.Repository.AddCalls);
        Assert.Empty(factory.ExternalService.RequestedIds);
    }

    [Fact]
    public async Task Get_WithEmptyCatalog_ReturnsOkAndEmptyArray()
    {
        using var factory = new CatalogApiFactory();
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync("/api/Game");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var result = await response.Content.ReadFromJsonAsync<List<Game>>();
        Assert.NotNull(result);
        Assert.Empty(result);
        Assert.Equal(1, factory.Repository.ReadAllCalls);
        Assert.Equal(0, factory.Repository.AddCalls);
        Assert.Empty(factory.ExternalService.RequestedIds);
    }

    [Fact]
    public async Task Post_WithExternalId_ReturnsCreatedAndPersistsGame()
    {
        using var factory = new CatalogApiFactory();
        factory.ExternalService.Result = new ExternalGameDTO
        {
            ExternalId = 42, Title = "Portal", CoverUrl = "https://example.test/portal.png",
            ReleaseDate = new DateTime(2007, 10, 10)
        };
        using var client = factory.CreateHttpsClient();

        using var response = await client.PostAsync("/api/Game?externalId=42", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Game>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(42, result.ExternalId);
        Assert.Equal("Portal", result.Title);
        Assert.Equal(factory.ExternalService.Result.CoverUrl, result.CoverUrl);
        Assert.Equal(factory.ExternalService.Result.ReleaseDate, result.ReleaseDate);
        var stored = Assert.Single(factory.Repository.Games);
        Assert.Equal(result.Id, stored.Id);
        Assert.Equal(result.ExternalId, stored.ExternalId);
        Assert.Equal(result.Title, stored.Title);
        Assert.Equal(result.CoverUrl, stored.CoverUrl);
        Assert.Equal(result.ReleaseDate, stored.ReleaseDate);
        Assert.Equal(1, factory.Repository.AddCalls);
        Assert.Equal(new[] { 42 }, factory.ExternalService.RequestedIds);
    }
}
