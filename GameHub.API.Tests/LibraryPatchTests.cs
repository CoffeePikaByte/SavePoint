using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GameHub.API.Tests;

public class LibraryPatchTests
{
    private static UserGame Seed(LibraryApiFactory factory, Guid? userId = null)
    {
        var item = new UserGame
        {
            Id = Guid.NewGuid(), UserId = userId ?? factory.UserId, GameId = factory.GameId,
            Status = GameStatus.Playing, HoursPlayed = 7,
            AddedAt = new DateTime(2026, 11, 12, 12, 0, 0, DateTimeKind.Utc)
        };
        factory.Library.Items.Add(item);
        return item;
    }

    private static Task<HttpResponseMessage> Patch(HttpClient client, string gameId, string json, string query = "") =>
        client.PatchAsync("/api/library/games/" + gameId + query, new StringContent(json, Encoding.UTF8, "application/json"));

    [Theory]
    [InlineData("{\"status\":2}", 2, 7)]
    [InlineData("{\"hoursPlayed\":0}", 1, 0)]
    [InlineData("{\"hoursPlayed\":null}", 1, null)]
    [InlineData("{\"status\":3,\"hoursPlayed\":12}", 3, 12)]
    [InlineData("{\"status\":1,\"hoursPlayed\":7}", 1, 7)]
    public async Task Patch_UpdatesOnlyOwnProvidedFieldsAndReturnsExistingDto(string json, int status, int? hours)
    {
        using var factory = new LibraryApiFactory();
        var item = Seed(factory);
        var otherUser = Guid.NewGuid();
        factory.ExistingUserIds.Add(otherUser);
        var other = Seed(factory, otherUser);
        using var client = factory.AuthenticatedClient();
        using var response = await Patch(client, factory.GameId.ToString(), json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var dto = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(item.Id, dto.GetProperty("id").GetGuid());
        Assert.Equal(factory.GameId, dto.GetProperty("gameId").GetGuid());
        Assert.Equal(status, dto.GetProperty("status").GetInt32());
        var value = dto.GetProperty("hoursPlayed");
        Assert.Equal(hours, value.ValueKind == JsonValueKind.Null ? (int?)null : value.GetInt32());
        Assert.Equal(item.AddedAt, dto.GetProperty("addedAt").GetDateTime());
        Assert.Equal(new[] { "addedAt", "gameId", "hoursPlayed", "id", "status" }, dto.EnumerateObject().Select(p => p.Name).OrderBy(name => name));
        Assert.Equal((GameStatus)status, item.Status);
        Assert.Equal(hours, item.HoursPlayed);
        Assert.Equal(factory.UserId, item.UserId);
        Assert.Equal(factory.GameId, item.GameId);
        Assert.Equal(new DateTime(2026, 11, 12, 12, 0, 0, DateTimeKind.Utc), item.AddedAt);
        Assert.Equal(GameStatus.Playing, other.Status);
        Assert.Equal(7, other.HoursPlayed);
        Assert.Equal(1, factory.Library.UpdateCalls);
        Assert.Equal(2, factory.Library.Items.Count);
        AssertNoExternalCallsOrInserts(factory);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"status\":null}")]
    [InlineData("{\"status\":4}")]
    [InlineData("{\"status\":-1}")]
    [InlineData("{\"status\":\"Playing\"}")]
    [InlineData("{\"status\":\"1\"}")]
    [InlineData("{\"status\":2,\"hoursPlayed\":-1}")]
    [InlineData("{\"hoursPlayed\":1.5}")]
    [InlineData("{\"hoursPlayed\":\"12\"}")]
    [InlineData("{\"hoursPlayed\":2147483648}")]
    [InlineData("{\"status\":2,\"userId\":\"00000000-0000-0000-0000-000000000001\"}")]
    [InlineData("{\"status\":2,\"addedAt\":null}")]
    [InlineData("{\"status\":2,\"gameId\":null}")]
    [InlineData("{\"status\":1,\"Status\":2}")]
    [InlineData("{\"hoursPlayed\":1,\"hoursPlayed\":null}")]
    public async Task Patch_InvalidBody_IsBadRequestWithNoMutationOrWrite(string json)
    {
        using var factory = new LibraryApiFactory();
        var item = Seed(factory);
        using var client = factory.AuthenticatedClient();
        using var response = await Patch(client, factory.GameId.ToString(), json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(GameStatus.Playing, item.Status);
        Assert.Equal(7, item.HoursPlayed);
        Assert.Equal(0, factory.Library.UpdateCalls);
        AssertNoExternalCallsOrInserts(factory);
    }

    [Theory]
    [InlineData("not-a-guid", "")]
    [InlineData("00000000-0000-0000-0000-000000000000", "")]
    [InlineData("valid", "?userId=someone-else")]
    [InlineData("valid", "?USERID=someone-else")]
    public async Task Patch_InvalidRouteOrUserOverride_IsBadRequest(string gameId, string query)
    {
        using var factory = new LibraryApiFactory();
        var item = Seed(factory);
        using var client = factory.AuthenticatedClient();
        using var response = await Patch(client, gameId == "valid" ? factory.GameId.ToString() : gameId, "{\"status\":2}", query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(GameStatus.Playing, item.Status);
        Assert.Equal(0, factory.Library.UpdateCalls);
        AssertNoExternalCallsOrInserts(factory);
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("expired")]
    [InlineData("bad-signature")]
    [InlineData("missing")]
    [InlineData("invalid-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Patch_Unauthorized_IsRejectedWithoutWrites(string kind)
    {
        using var factory = new LibraryApiFactory();
        var item = Seed(factory);
        using var client = kind == "anonymous" ? factory.CreateHttpsClient() : factory.AuthenticatedClient(
            kind is "expired" or "bad-signature" ? null : kind, expired: kind == "expired", validSignature: kind != "bad-signature");
        using var response = await Patch(client, factory.GameId.ToString(), "{\"status\":2}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(GameStatus.Playing, item.Status);
        Assert.Equal(0, factory.Library.UpdateCalls);
        AssertNoExternalCallsOrInserts(factory);
    }

    [Theory]
    [InlineData("deleted-user")]
    [InlineData("other-owner")]
    [InlineData("not-in-library")]
    public async Task Patch_MissingUserOrOwnEntry_IsNotFound(string kind)
    {
        using var factory = new LibraryApiFactory();
        var other = Seed(factory, Guid.NewGuid());
        using var client = factory.AuthenticatedClient(kind == "deleted-user" ? Guid.NewGuid().ToString() : null);
        using var response = await Patch(client, kind == "not-in-library" ? Guid.NewGuid().ToString() : factory.GameId.ToString(), "{\"status\":2}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.False(string.IsNullOrEmpty((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()));
        Assert.Equal(GameStatus.Playing, other.Status);
        Assert.Equal(0, factory.Library.UpdateCalls);
        AssertNoExternalCallsOrInserts(factory);
    }

    private static void AssertNoExternalCallsOrInserts(LibraryApiFactory factory)
    {
        Assert.Equal(0, factory.Library.AddCalls);
        Assert.Equal(0, factory.Repository.AddCalls);
        Assert.Empty(factory.ExternalService.RequestedIds);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Patch_OnlyConfirmedMissingEntryErrorsAreNotFound(bool confirmedMissing)
    {
        using var factory = new LibraryApiFactory();
        Seed(factory);
        factory.Library.UpdateError = confirmedMissing ? new LibraryGameNotFoundException() :
            new DbUpdateConcurrencyException("Unconfirmed concurrency failure");
        using var client = factory.AuthenticatedClient();
        using var response = await Patch(client, factory.GameId.ToString(), "{\"status\":2}");
        Assert.Equal(confirmedMissing ? HttpStatusCode.NotFound : HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(1, factory.Library.UpdateCalls);
        AssertNoExternalCallsOrInserts(factory);
    }
}
