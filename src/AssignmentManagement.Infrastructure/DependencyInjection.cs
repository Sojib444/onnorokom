using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Infrastructure.Authentication;
using AssignmentManagement.Infrastructure.Persistence.Context;
using AssignmentManagement.Infrastructure.Persistence.Repositories.Read;
using AssignmentManagement.Infrastructure.Persistence.Repositories.Write;
using AssignmentManagement.Infrastructure.Persistence.Seed;
using AssignmentManagement.Infrastructure.Persistence.UnitOfWork;
using AssignmentManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentManagement.Infrastructure;

/// <summary>
/// Composition-root registration for infrastructure concerns: EF Core with PostgreSQL
/// (Npgsql) split into a tracking write context and an untracked read context, the
/// read/write repositories, the unit of work, JWT and password hashing, and file
/// storage. Called from <c>Program.cs</c>, the one place the API is allowed to touch
/// infrastructure types.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");
        }

        // The base AppDbContext stays registered for schema maintenance: Program.cs
        // applies migrations through it, and integration tests drive it directly.
        // Read and write flows use the specialized contexts below so query-side reads
        // never pollute the write-side change tracker.
        // AppDbContext's constructor takes non-generic DbContextOptions, so it is
        // registered explicitly (rather than via AddDbContext) to keep its options from
        // being clobbered by the derived contexts' registrations below.
        services.AddScoped<AppDbContext>(_ => new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString)
                .Options));
        services.AddDbContext<WriteDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddDbContext<ReadDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddHttpContextAccessor();

        services.AddScoped<IUserReadRepository, UserReadRepository>();
        services.AddScoped<IUserWriteRepository, UserWriteRepository>();
        services.AddScoped<IClassReadRepository, ClassReadRepository>();
        services.AddScoped<IClassWriteRepository, ClassWriteRepository>();
        services.AddScoped<ISubjectReadRepository, SubjectReadRepository>();
        services.AddScoped<ISubjectWriteRepository, SubjectWriteRepository>();
        services.AddScoped<ITeacherAssignmentReadRepository, TeacherAssignmentReadRepository>();
        services.AddScoped<ITeacherAssignmentWriteRepository, TeacherAssignmentWriteRepository>();
        services.AddScoped<IAssignmentReadRepository, AssignmentReadRepository>();
        services.AddScoped<IAssignmentWriteRepository, AssignmentWriteRepository>();
        services.AddScoped<ISubmissionReadRepository, SubmissionReadRepository>();
        services.AddScoped<ISubmissionWriteRepository, SubmissionWriteRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.Configure<Authentication.JwtOptions>(
            configuration.GetSection(Authentication.JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenService, Authentication.JwtTokenService>();
        services.AddScoped<ICurrentUser, Authentication.CurrentUser>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<DatabaseSeeder>();

        var storageRoot = configuration["FileStorage:RootPath"] ?? "uploads";
        services.AddSingleton<IFileStorage>(new LocalFileStorage(
            Path.GetFullPath(storageRoot)));

        return services;
    }
}
