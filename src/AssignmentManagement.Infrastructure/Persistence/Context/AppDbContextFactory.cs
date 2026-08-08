using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AssignmentManagement.Infrastructure.Persistence.Context;

/// <summary>
/// Creates an <see cref="AppDbContext"/> for <c>dotnet ef</c> commands without booting
/// the API host. The connection string is read from the <c>DB_CONNECTION_STRING</c>
/// environment variable (the same one Docker and CI use); when it is missing a local
/// development placeholder is used because generating a migration does not require a
/// live connection.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=AssignmentManagement;Username=postgres;Password=DevOnly_Change_123!";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
