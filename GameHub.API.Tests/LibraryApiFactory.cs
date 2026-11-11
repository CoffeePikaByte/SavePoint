using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using GameHub.Application.Interfaces.Repositories;
using GameHub.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace GameHub.API.Tests;

public sealed class LibraryApiFactory : CatalogApiFactory
{
    public Guid UserId { get; } = Guid.NewGuid();
    public Guid GameId { get; } = Guid.NewGuid();
    public HashSet<Guid> ExistingUserIds { get; } = new();
    public LibraryRepository Library { get; }

    public LibraryApiFactory()
    {
        Repository.Seed(new Game { Id = GameId, Title = "Portal" });
        ExistingUserIds.Add(UserId);
        Library = new LibraryRepository(Repository.Games);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.AddSingleton<IUserRepository>(new Users(ExistingUserIds));
            services.RemoveAll<IUserGameRepository>();
            services.AddSingleton<IUserGameRepository>(Library);
            // Program captures JWT settings before WebApplicationFactory's configuration
            // overrides. Set the test key explicitly without bypassing JWT validation.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("test-only-signing-key-not-a-real-secret-123456789"));
                options.TokenValidationParameters.ValidIssuer = "SavePoint.Tests";
                options.TokenValidationParameters.ValidAudience = "SavePoint.Tests";
            });
        });
    }

    public HttpClient AuthenticatedClient(string? identity = null, bool expired = false, bool validSignature = true)
    {
        var client = CreateHttpsClient();
        var claims = identity == "missing" ? Array.Empty<Claim>() :
            new[] { new Claim(ClaimTypes.NameIdentifier, identity ?? UserId.ToString()) };
        var key = validSignature ? "test-only-signing-key-not-a-real-secret-123456789" :
            "different-test-only-signing-key-not-a-secret-123456789";
        var token = new JwtSecurityToken("SavePoint.Tests", "SavePoint.Tests", claims,
            expires: expired ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    private sealed class Users(HashSet<Guid> ids) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid userId) =>
            Task.FromResult(ids.Contains(userId) ? new User { Id = userId } : null);
        public Task<User?> GetByEmailAsync(string email) => throw new NotSupportedException();
        public Task AddAsync(User user) => throw new NotSupportedException();
    }

    public sealed class LibraryRepository(IReadOnlyList<Game> games) : IUserGameRepository
    {
        public List<UserGame> Items { get; } = new();
        public Exception? SaveError { get; set; }
        public int AddCalls { get; private set; }
        public int ListReads { get; private set; }
        public Task<UserGame?> GetAsync(Guid userId, Guid gameId) =>
            Task.FromResult(Items.SingleOrDefault(item => item.UserId == userId && item.GameId == gameId));
        public Task AddAsync(UserGame item)
        {
            AddCalls++;
            if (SaveError is not null) throw SaveError;
            Items.Add(item);
            return Task.CompletedTask;
        }
        public Task<List<UserGame>> GetByUserIdAsync(Guid id)
        {
            ListReads++;
            return Task.FromResult(Items.Where(item => item.UserId == id)
                .OrderByDescending(item => item.AddedAt).ThenBy(item => item.Id)
                .Select(item => new UserGame
                {
                    Id = item.Id, UserId = item.UserId, GameId = item.GameId,
                    Game = games.Single(game => game.Id == item.GameId),
                    Status = item.Status, HoursPlayed = item.HoursPlayed, AddedAt = item.AddedAt
                }).ToList());
        }
        public Task UpdateAsync(UserGame item) => throw new NotSupportedException();
        public Task DeleteAsync(UserGame item) => throw new NotSupportedException();
    }
}
