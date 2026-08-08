using MediatR;

namespace AssignmentManagement.Domain.Common;

/// <summary>
/// Marker interface for domain events. A domain event records something meaningful
/// that happened in the domain; it is a fact, not a command. Implementing
/// <see cref="INotification"/> lets the application layer dispatch these events through
/// MediatR without the domain referencing the mediator implementation.
/// </summary>
public interface IDomainEvent : INotification
{
}
