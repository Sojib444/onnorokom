using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace AssignmentManagement.IntegrationTests;

/// <summary>
/// Boots the real API on a TestServer against a dedicated PostgreSQL test database.
/// The test database is created on demand and migrations run on startup, so the tests
/// exercise the full HTTP pipeline, EF Core persistence and the domain rules together.
/// </summary>
public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    public const string DatabaseName = "AssignmentManagement_Test";

    private const string PostgresPassword = "DevOnly_Change_123!";

    public static string ConnectionString =>
        $"Host=localhost;Port=5432;Database={DatabaseName};Username=postgres;Password={PostgresPassword}";

    public static string UploadsPath => Path.Combine(
        Path.GetTempPath(), "opencode", "itest-uploads");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        EnsureTestDatabaseExists();

        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Seed:RunOnStartup", "false");
        builder.UseSetting("FileStorage:RootPath", UploadsPath);
    }

    private static void EnsureTestDatabaseExists()
    {
        const string maintenance =
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=DevOnly_Change_123!";

        using var connection = new NpgsqlConnection(maintenance);
        connection.Open();

        using var check = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @name", connection);
        check.Parameters.AddWithValue("name", DatabaseName);

        if (check.ExecuteScalar() is not null)
        {
            return;
        }

        using var create = new NpgsqlCommand($"CREATE DATABASE \"{DatabaseName}\"", connection);
        create.ExecuteNonQuery();
    }
}
