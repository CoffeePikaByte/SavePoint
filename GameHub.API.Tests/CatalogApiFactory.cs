using GameHub.Application.DTOs.Games;
using GameHub.Application.Interfaces.Repositories;
using GameHub.Application.Interfaces.Services;
using GameHub.Domain.Entities;
using GameHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameHub.API.Tests;

public sealed class CatalogApiFactory : WebApplicationFactory<Program>
{
    public InMemoryGameRepository Repository { get; } = new();
    public FakeExternalGameService ExternalService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Do not load user secrets or require real application settings.
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.Sources.Clear();
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test-only-signing-key-not-a-real-secret-123456789",
                ["Jwt:Issuer"] = "SavePoint.Tests",
                ["Jwt:Audience"] = "SavePoint.Tests"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IGameRepository>();
            services.AddSingleton<IGameRepository>(Repository);
            services.RemoveAll<IExternalGameService>();
            services.AddSingleton<IExternalGameService>(ExternalService);

            // No database provider is available in this host. Accidental database
            // use must fail rather than connect to PostgreSQL.
            services.RemoveAll<GameHubDbContext>();
            services.RemoveAll<DbContextOptions<GameHubDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<GameHubDbContext>>();
            services.AddDbContext<GameHubDbContext>();
        });
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });
}

public sealed class InMemoryGameRepository : IGameRepository
{
    private readonly List<Game> _games = new();
    public IReadOnlyList<Game> Games => _games;
    public int ReadAllCalls { get; private set; }
    public int AddCalls { get; private set; }

    public void Seed(params Game[] games) => _games.AddRange(games);

    public Task<List<Game>> GetAllGamesAsync()
    {
        ReadAllCalls++;
        return Task.FromResult(new List<Game>(_games));
    }

    public Task<Game?> GetByIdAsync(Guid id) =>
        Task.FromResult(_games.SingleOrDefault(game => game.Id == id));

    public Task<Game?> GetByExternalIdAsync(int externalId) =>
        Task.FromResult(_games.SingleOrDefault(game => game.ExternalId == externalId));

    public Task AddAsync(Game game)
    {
        AddCalls++;
        _games.Add(game);
        return Task.CompletedTask;
    }
}

public sealed class FakeExternalGameService : IExternalGameService
{
    public ExternalGameDTO? Result { get; set; }
    public List<int> RequestedIds { get; } = new();

    public Task<ExternalGameDTO?> GetGameAsync(int externalId)
    {
        RequestedIds.Add(externalId);
        if (Result is null)
        {
            throw new InvalidOperationException("External service must not be called for catalog reads.");
        }
        return Task.FromResult<ExternalGameDTO?>(Result);
    }
}
