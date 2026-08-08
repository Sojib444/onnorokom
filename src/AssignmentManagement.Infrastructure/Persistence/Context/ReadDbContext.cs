using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Context;

/// <summary>
/// The read-only EF Core context used by the query-side repositories. It shares the
/// model of <see cref="AppDbContext"/> and turns off change tracking globally, so every
/// query run through it is <c>AS NO TRACKING</c> and never registers entities in a
/// change tracker.
/// </summary>
public sealed class ReadDbContext : AppDbContext
{
    public ReadDbContext(DbContextOptions<ReadDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }
}
