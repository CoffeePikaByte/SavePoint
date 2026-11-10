using System.Text.RegularExpressions;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameHub.Infrastructure.IntegrationTests;

internal sealed record IntegrationSettings(string ConnectionString, string RunId, string Token)
{
    public static IntegrationSettings FromEnvironment() => Parse(
        Environment.GetEnvironmentVariable("SAVEPOINT_POSTGRES_TESTS"),
        Environment.GetEnvironmentVariable("SAVEPOINT_TEST_CONNECTION"),
        Environment.GetEnvironmentVariable("SAVEPOINT_TEST_RUN_ID"),
        Environment.GetEnvironmentVariable("SAVEPOINT_TEST_TOKEN"));

    public static IntegrationSettings Parse(string? enable, string? connection, string? run, string? token)
    {
        if (enable != "1" || string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Integration tests require SAVEPOINT_POSTGRES_TESTS=1 and a script-generated SAVEPOINT_TEST_CONNECTION. Use scripts/test-postgres.sh.");
        if (run is null || !Regex.IsMatch(run, "\\A[a-f0-9]{32}\\z") ||
            token is null || !Regex.IsMatch(token, "\\A[a-f0-9]{64}\\z"))
            throw new InvalidOperationException("Missing or invalid integration ownership identity.");
        var builder = new NpgsqlConnectionStringBuilder(connection);
        if (builder.Host != "127.0.0.1" || builder.Port < 1024 || builder.Port == 5433 ||
            builder.Database != "spit_" + run || builder.Username != "spit_admin" ||
            string.IsNullOrEmpty(builder.Password) || builder.Pooling)
            throw new InvalidOperationException("Refusing a connection that does not match the disposable integration environment.");
        builder.Timeout = 5;
        builder.CommandTimeout = 15;
        builder.Options = "-c statement_timeout=15000 -c lock_timeout=10000";
        return new(builder.ConnectionString, run, token);
    }

    public async Task<NpgsqlConnection> OpenOwnedAdminAsync()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        try
        {
            await connection.OpenAsync();
            await using (var version = new NpgsqlCommand("SHOW server_version_num", connection))
            {
                var number = int.Parse((string)(await version.ExecuteScalarAsync())!);
                if (number < 160000 || number >= 170000)
                    throw new InvalidOperationException("The integration server must be PostgreSQL 16.");
            }
            await using var command = new NpgsqlCommand(
                "SELECT EXISTS (SELECT 1 FROM public.test_run_identity WHERE run_id = @run AND token = @token)", connection);
            command.Parameters.AddWithValue("run", RunId);
            command.Parameters.AddWithValue("token", Token);
            if (await command.ExecuteScalarAsync() is not true)
                throw new InvalidOperationException("Database ownership marker does not match this test run.");
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}

internal sealed class TestDatabase : IAsyncDisposable
{
    private readonly IntegrationSettings _settings;
    private readonly string _name;
    private bool _created;
    public Guid UserId { get; } = Guid.NewGuid();
    public Guid OtherUserId { get; } = Guid.NewGuid();
    public Guid GameId { get; } = Guid.NewGuid();

    private TestDatabase(IntegrationSettings settings)
    {
        _settings = settings;
        _name = $"spit_{settings.RunId[..12]}_{Guid.NewGuid():N}";
    }

    public static async Task<TestDatabase> CreateAsync()
    {
        var database = new TestDatabase(IntegrationSettings.FromEnvironment());
        try
        {
            await using (var admin = await database._settings.OpenOwnedAdminAsync())
            {
                await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database._name}\"", admin);
                await create.ExecuteNonQueryAsync();
                database._created = true;
            }
            await using var context = database.CreateContext();
            await context.Database.MigrateAsync();
            var expected = context.Database.GetMigrations().ToArray();
            var actual = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
            if (expected.Length != 2 || !expected.SequenceEqual(actual))
                throw new InvalidOperationException("The disposable database did not apply the expected migration history.");
            context.Users.AddRange(database.NewUser(database.UserId), database.NewUser(database.OtherUserId));
            context.Games.Add(new Game { Id = database.GameId, ExternalId = 1, Title = "Integration-only game" });
            await context.SaveChangesAsync();
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    public GameHubDbContext CreateContext()
    {
        if (!_created) throw new InvalidOperationException("Test database is not active.");
        var builder = new NpgsqlConnectionStringBuilder(_settings.ConnectionString) { Database = _name };
        return new GameHubDbContext(new DbContextOptionsBuilder<GameHubDbContext>()
            .UseNpgsql(builder.ConnectionString, options => options.CommandTimeout(15)).Options);
    }

    public UserGame NewEntry() => new()
    {
        Id = Guid.NewGuid(), UserId = UserId, GameId = GameId,
        Status = GameStatus.BackLog, HoursPlayed = null,
        // PostgreSQL timestamps have microsecond precision; use a fixed UTC millisecond value.
        AddedAt = new DateTime(2026, 11, 10, 12, 0, 0, 123, DateTimeKind.Utc)
    };

    private User NewUser(Guid id) => new()
    {
        Id = id, UserName = "integration-user", Email = $"{id:N}@example.test",
        PasswordHash = "test-only-placeholder-not-an-authentication-test", CreatedAt = DateTime.UtcNow
    };

    public async ValueTask DisposeAsync()
    {
        if (!_created) return;
        if (!Regex.IsMatch(_name, $"\\Aspit_{_settings.RunId[..12]}_[a-f0-9]{{32}}\\z"))
            throw new InvalidOperationException("Refusing to delete an unowned database name.");
        await using var admin = await _settings.OpenOwnedAdminAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE \"{_name}\"", admin);
        await drop.ExecuteNonQueryAsync();
        await using var check = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @name)", admin);
        check.Parameters.AddWithValue("name", _name);
        if (await check.ExecuteScalarAsync() is not false)
            throw new InvalidOperationException("Test database cleanup could not be verified.");
        _created = false;
    }
}
