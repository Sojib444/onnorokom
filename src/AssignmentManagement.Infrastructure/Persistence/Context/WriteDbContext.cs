using AssignmentManagement.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Persistence.Context;

/// <summary>
/// The tracking EF Core context used by the write-side repositories and the unit of
/// work. It shares the model of <see cref="AppDbContext"/> and adds the one
/// cross-cutting concern that does not belong in the domain: dispatching the domain
/// events raised by aggregates after a successful save, so the transactional save and
/// its side effects stay in one operation.
/// </summary>
public sealed class WriteDbContext : AppDbContext
{
    private readonly IPublisher _publisher;

    public WriteDbContext(DbContextOptions<WriteDbContext> options, IPublisher publisher)
        : base(options)
    {
        _publisher = publisher;
    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var pendingEvents = CollectDomainEvents();
        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await DispatchDomainEventsAsync(pendingEvents, cancellationToken).ConfigureAwait(false);

        return result;
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        var pending = ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                var events = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();
                return events;
            })
            .ToList();

        return pending;
    }

    private async Task DispatchDomainEventsAsync(
        List<IDomainEvent> domainEvents,
        CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
