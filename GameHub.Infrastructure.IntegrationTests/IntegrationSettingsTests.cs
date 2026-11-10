namespace GameHub.Infrastructure.IntegrationTests;

public class IntegrationSettingsTests
{
    private const string Run = "0123456789abcdef0123456789abcdef";
    private static readonly string Token = new('a', 64);
    private const string Connection = "Host=127.0.0.1;Port=55432;Database=spit_0123456789abcdef0123456789abcdef;Username=spit_admin;Password=test-only;Pooling=false";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    public void Parse_WithoutExplicitEnable_RejectsBeforeConnecting(string? enable) =>
        Assert.Throws<InvalidOperationException>(() => IntegrationSettings.Parse(enable, Connection, Run, Token));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Parse_WithoutGeneratedConnection_Rejects(string? connection) =>
        Assert.Throws<InvalidOperationException>(() => IntegrationSettings.Parse("1", connection, Run, Token));

    [Theory]
    [InlineData("Host=development")]
    [InlineData("Database=gamehub")]
    [InlineData("Username=postgres")]
    [InlineData("Port=5433")]
    [InlineData("Pooling=true")]
    public void Parse_UnexpectedConnectionIdentity_Rejects(string replacement)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(Connection);
        var parts = replacement.Split('=');
        builder[parts[0]] = parts[1];
        Assert.Throws<InvalidOperationException>(() => IntegrationSettings.Parse("1", builder.ConnectionString, Run, Token));
    }

    [Fact]
    public void Parse_WithoutOwnershipToken_Rejects() =>
        Assert.Throws<InvalidOperationException>(() => IntegrationSettings.Parse("1", Connection, Run, null));

    [Fact]
    public void Parse_WithOwnedSettings_AcceptsWithoutOpeningAConnection()
    {
        var settings = IntegrationSettings.Parse("1", Connection, Run, Token);
        Assert.Equal(Run, settings.RunId);
        Assert.Equal(Token, settings.Token);
    }
}
