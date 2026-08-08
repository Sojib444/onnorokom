using System.Text;
using AssignmentManagement.Api.Middleware;
using AssignmentManagement.Application;
using AssignmentManagement.Infrastructure;
using AssignmentManagement.Infrastructure.Authentication;
using AssignmentManagement.Infrastructure.Persistence.Context;
using AssignmentManagement.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---- Structured logging -----------------------------------------------------
// Serilog is configured from appsettings.json and enriched with the request scope so
// every log line carries context. Passwords and tokens are never logged.
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

// ---- Clean Architecture wiring ----------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---- JWT authentication ------------------------------------------------------
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        $"The JWT configuration section '{JwtOptions.SectionName}' is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.Secret) || jwtOptions.Secret.Length < 32)
{
    throw new InvalidOperationException(
        "The JWT secret must be configured and at least 32 characters long. " +
        "Set 'Jwt:Secret' in configuration or the JWT__SECRET environment variable.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) || string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException(
        "JWT issuer and audience must be configured. Set 'Jwt:Issuer' and 'Jwt:Audience'.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();

// ---- MVC ---------------------------------------------------------------------
builder.Services.AddControllers();

// ---- Swagger / OpenAPI --------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Assignment & Submission Management API",
        Version = "v1",
        Description =
            "Role-based API for teachers, students and administrators. " +
            "Authenticate with the login endpoint, copy the returned access token into " +
            "the Authorize dialog, and call role-protected endpoints.",
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token returned by POST /api/auth/login.",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });

    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// ---- CORS ---------------------------------------------------------------------
// Accept a comma-separated allow-list (e.g. the Cors__AllowedOrigins environment
// variable used by Docker Compose) or fall back to the indexed array from
// appsettings.json, which is the default for local development.
var corsOrigins = builder.Configuration["Cors:AllowedOrigins"];
var allowedOrigins = string.IsNullOrWhiteSpace(corsOrigins)
    ? builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? []
    : corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

// ---- Health checks ------------------------------------------------------------
builder.Services.AddHealthChecks();

var app = builder.Build();

// ---- Database migration and seeding -------------------------------------------
// Migrations are applied on startup so the application can be brought up with a single
// command in Docker or locally. The seeder is idempotent and gated by configuration.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<AppDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    await dbContext.Database.MigrateAsync();

    if (builder.Configuration.GetValue<bool>("Seed:RunOnStartup"))
    {
        var seeder = services.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync(CancellationToken.None);
    }

    logger.LogInformation("Database migration and seeding finished.");
}

// ---- HTTP pipeline -------------------------------------------------------------
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (builder.Configuration.GetValue("Api:EnableSwagger", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Assignment Management API v1");
    });
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>Entry point marker for the web host.</summary>
public partial class Program;
