using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;

namespace GameHub.API.Tests;

public class LibraryGetTests
{
    private const string Route = "/api/library/games";

    [Fact]
    public async Task Get_UsesJwtUserAndReturnsOrderedFlatGameDetailsWithoutWrites()
    {
        using var factory = new LibraryApiFactory();
        var otherUserId = Guid.NewGuid();
        factory.ExistingUserIds.Add(otherUserId);
        var games = new[]
        {
            new Game { Id = Guid.NewGuid(), Title = "Celeste", CoverUrl = "https://example.test/cover.png", ReleaseDate = new DateTime(2018, 1, 25) },
            new Game { Id = Guid.NewGuid(), Title = "Hades" }
        };
        factory.Repository.Seed(games);
        var time = new DateTime(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);
        var older = new UserGame { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), UserId = factory.UserId, GameId = factory.GameId, AddedAt = time.AddDays(-1) };
        var tieFirst = new UserGame { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), UserId = factory.UserId, GameId = games[0].Id, AddedAt = time, Status = GameStatus.Playing, HoursPlayed = 12 };
        var tieLast = new UserGame { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), UserId = factory.UserId, GameId = games[1].Id, AddedAt = time, Status = GameStatus.Complete };
        var other = new UserGame { Id = Guid.NewGuid(), UserId = otherUserId, GameId = factory.GameId, AddedAt = time.AddDays(1) };
        factory.Library.Items.AddRange(new[] { older, tieLast, other, tieFirst });
        var before = factory.Library.Items.Select(item => (item.Id, item.UserId, item.GameId, item.Status, item.HoursPlayed, item.AddedAt)).ToArray();
        using var client = factory.AuthenticatedClient();

        // An untrusted query parameter must never override the authenticated identity.
        using var response = await client.GetAsync(Route + "?userId=" + otherUserId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.EnumerateArray().ToArray();
        var expected = new[] { tieFirst, tieLast, older };
        Assert.Equal(expected.Select(item => item.Id), items.Select(item => item.GetProperty("id").GetGuid()));
        for (var index = 0; index < items.Length; index++)
        {
            var actual = items[index];
            var entry = expected[index];
            var game = factory.Repository.Games.Single(game => game.Id == entry.GameId);
            Assert.Equal(entry.GameId, actual.GetProperty("gameId").GetGuid());
            Assert.Equal(game.Title, actual.GetProperty("title").GetString());
            Assert.Equal(game.CoverUrl, actual.GetProperty("coverUrl").GetString());
            var release = actual.GetProperty("releaseDate");
            Assert.Equal(game.ReleaseDate, release.ValueKind == JsonValueKind.Null ? (DateTime?)null : release.GetDateTime());
            Assert.Equal((int)entry.Status, actual.GetProperty("status").GetInt32());
            var hours = actual.GetProperty("hoursPlayed");
            Assert.Equal(entry.HoursPlayed, hours.ValueKind == JsonValueKind.Null ? (int?)null : hours.GetInt32());
            Assert.Equal(entry.AddedAt, actual.GetProperty("addedAt").GetDateTime());
            Assert.Equal(new[] { "addedAt", "coverUrl", "gameId", "hoursPlayed", "id", "releaseDate", "status", "title" },
                actual.EnumerateObject().Select(property => property.Name).OrderBy(name => name));
        }
        using var otherClient = factory.AuthenticatedClient(otherUserId.ToString());
        using var otherResponse = await otherClient.GetAsync(Route);
        Assert.Equal(HttpStatusCode.OK, otherResponse.StatusCode);
        var otherBody = await otherResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(other.Id, Assert.Single(otherBody.EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal(before, factory.Library.Items.Select(item => (item.Id, item.UserId, item.GameId, item.Status, item.HoursPlayed, item.AddedAt)).ToArray());
        AssertNoWritesOrRawg(factory);
    }

    [Fact]
    public async Task Get_EmptyLibraryWithOtherUsersData_ReturnsEmptyArray()
    {
        using var factory = new LibraryApiFactory();
        factory.Library.Items.Add(new UserGame { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), GameId = factory.GameId });
        using var client = factory.AuthenticatedClient();
        using var response = await client.GetAsync(Route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Empty((await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
        Assert.Single(factory.Library.Items);
        AssertNoWritesOrRawg(factory);
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("expired")]
    [InlineData("bad-signature")]
    [InlineData("missing")]
    [InlineData("invalid-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Get_InvalidAuthenticationOrIdentity_ReturnsUnauthorizedBeforeLibraryRead(string kind)
    {
        using var factory = new LibraryApiFactory();
        using var client = kind == "anonymous" ? factory.CreateHttpsClient() :
            factory.AuthenticatedClient(kind is "expired" or "bad-signature" ? null : kind,
                expired: kind == "expired", validSignature: kind != "bad-signature");
        using var response = await client.GetAsync(Route);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.Library.ListReads);
        AssertNoWritesOrRawg(factory);
    }

    [Fact]
    public async Task Get_DeletedUser_ReturnsExistingNotFoundContract()
    {
        using var factory = new LibraryApiFactory();
        using var client = factory.AuthenticatedClient(Guid.NewGuid().ToString());
        using var response = await client.GetAsync(Route);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Usuario no encontrado.", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
        Assert.Equal(0, factory.Library.ListReads);
        AssertNoWritesOrRawg(factory);
    }

    private static void AssertNoWritesOrRawg(LibraryApiFactory factory)
    {
        Assert.Equal(0, factory.Library.AddCalls);
        Assert.Equal(0, factory.Repository.AddCalls);
        Assert.Empty(factory.ExternalService.RequestedIds);
    }
}
