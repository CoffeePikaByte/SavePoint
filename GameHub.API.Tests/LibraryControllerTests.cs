using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameHub.Application.Exceptions;

namespace GameHub.API.Tests;

public class LibraryControllerTests
{
    private const string Route = "/api/library/games";

    [Fact]
    public async Task Post_UsesJwtIdentityAndReturnsCreatedEntry()
    {
        using var factory = new LibraryApiFactory();
        using var client = factory.AuthenticatedClient();
        var before = DateTime.UtcNow;

        using var response = await client.PostAsJsonAsync(Route, new { gameId = factory.GameId, userId = Guid.NewGuid() });

        Assert.True(response.StatusCode == HttpStatusCode.Created, response.ToString());
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var stored = Assert.Single(factory.Library.Items);
        Assert.NotEqual(Guid.Empty, stored.Id);
        Assert.Equal(factory.UserId, stored.UserId);
        Assert.Equal(factory.GameId, stored.GameId);
        Assert.Equal(0, (int)stored.Status);
        Assert.Null(stored.HoursPlayed);
        Assert.Equal(DateTimeKind.Utc, stored.AddedAt.Kind);
        Assert.InRange(stored.AddedAt, before, DateTime.UtcNow);
        Assert.Equal(stored.Id, body.GetProperty("id").GetGuid());
        Assert.Equal(stored.GameId, body.GetProperty("gameId").GetGuid());
        Assert.Equal(0, body.GetProperty("status").GetInt32());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("hoursPlayed").ValueKind);
        Assert.Equal(stored.AddedAt, body.GetProperty("addedAt").GetDateTime());
        Assert.False(body.TryGetProperty("user", out _));
        Assert.False(body.TryGetProperty("game", out _));
        AssertNoCatalogMutationOrRawg(factory);
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("expired")]
    [InlineData("bad-signature")]
    [InlineData("missing")]
    [InlineData("invalid-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Post_InvalidAuthenticationOrIdentity_ReturnsUnauthorized(string kind)
    {
        using var factory = new LibraryApiFactory();
        using var client = kind == "anonymous" ? factory.CreateHttpsClient() :
            factory.AuthenticatedClient(kind is "expired" or "bad-signature" ? null : kind,
                expired: kind == "expired", validSignature: kind != "bad-signature");
        using var response = await client.PostAsJsonAsync(Route, new { gameId = factory.GameId });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(factory.Library.Items);
        AssertNoCatalogMutationOrRawg(factory);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"gameId\":null}")]
    [InlineData("{\"gameId\":\"not-a-guid\"}")]
    [InlineData("{\"gameId\":\"00000000-0000-0000-0000-000000000000\"}")]
    public async Task Post_InvalidBody_ReturnsBadRequest(string json)
    {
        using var factory = new LibraryApiFactory();
        using var client = factory.AuthenticatedClient();
        using var body = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(Route, body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Library.Items);
        AssertNoCatalogMutationOrRawg(factory);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Post_MissingGameOrUser_ReturnsNotFound(bool missingUser)
    {
        using var factory = new LibraryApiFactory();
        using var client = factory.AuthenticatedClient(missingUser ? Guid.NewGuid().ToString() : null);
        using var response = await client.PostAsJsonAsync(Route, new { gameId = missingUser ? factory.GameId : Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(missingUser ? "Usuario no encontrado." : "Game not found in the local catalog.",
            error.GetProperty("message").GetString());
        Assert.Empty(factory.Library.Items);
        AssertNoCatalogMutationOrRawg(factory);
    }

    [Fact]
    public async Task Post_Duplicate_ReturnsConflictAndKeepsOneEntry()
    {
        using var factory = new LibraryApiFactory();
        using var client = factory.AuthenticatedClient();
        using var first = await client.PostAsJsonAsync(Route, new { gameId = factory.GameId });
        using var duplicate = await client.PostAsJsonAsync(Route, new { gameId = factory.GameId });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Single(factory.Library.Items);
        AssertNoCatalogMutationOrRawg(factory);
    }

    private static void AssertNoCatalogMutationOrRawg(LibraryApiFactory factory)
    {
        Assert.Equal(0, factory.Repository.AddCalls);
        Assert.Empty(factory.ExternalService.RequestedIds);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_SaveFailure_OnlyLibraryDuplicatesReturnConflict(bool duplicate)
    {
        using var factory = new LibraryApiFactory();
        factory.Library.SaveError = duplicate ? new GameAlreadyInLibraryException() : new InvalidOperationException("Storage failure");
        using var client = factory.AuthenticatedClient();
        using var response = await client.PostAsJsonAsync(Route, new { gameId = factory.GameId });
        Assert.Equal(duplicate ? HttpStatusCode.Conflict : HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Empty(factory.Library.Items);
        AssertNoCatalogMutationOrRawg(factory);
    }
}
