using AssignmentManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Context;

/// <summary>
/// Base EF Core context for the system. It maps the domain aggregates to PostgreSQL
/// (via Npgsql) and owns the shared model: all <see cref="DbSet{TEntity}"/> members and
/// the entity configurations applied in <see cref="OnModelCreating"/>.
/// </summary>
/// <remarks>
/// Audit timestamps (<see cref="Domain.Common.IAuditable"/>) are owned by the domain:
/// every aggregate sets its own CreatedAt/UpdatedAt through its constructors and
/// mutating methods, so this context never needs to touch them.
/// <para>
/// This type is deliberately a base for the two specialized contexts and exists so the
/// EF Core migrations stay bound to a single model:
/// </para>
/// <list type="bullet">
/// <item><see cref="WriteDbContext"/> — tracking context; dispatches domain events
/// after a successful save and is used by the write repositories and unit of work.</item>
/// <item><see cref="ReadDbContext"/> — configures <see cref="ChangeTracker.QueryTrackingBehavior"/>
/// to <see cref="QueryTrackingBehavior.NoTracking"/> for the query-side repositories.</item>
/// </list>
/// </remarks>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionAttachment> SubmissionAttachments => Set<SubmissionAttachment>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
