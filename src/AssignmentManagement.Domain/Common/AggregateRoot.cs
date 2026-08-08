namespace AssignmentManagement.Domain.Common;

/// <summary>
/// Base class for aggregate roots. An aggregate root is the entry point to an
/// aggregate: all operations that mutate the aggregate's state must go through it,
/// and it is responsible for protecting the aggregate's invariants.
/// </summary>
/// <remarks>
/// Aggregate roots additionally collect <see cref="IDomainEvent"/>s raised during a
/// business operation. The application layer dispatches them after the transaction
/// commits.
/// </remarks>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Events raised by the aggregate during business operations. Not yet dispatched.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    /// <summary>
    /// Records a domain event raised while protecting an invariant. Events are
    /// dispatched once by the application layer.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Clears the collected domain events after they have been dispatched.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
