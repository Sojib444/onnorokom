using AssignmentManagement.Domain.Common;

namespace AssignmentManagement.Domain.Events;

/// <summary>
/// Raised when a teacher publishes an assignment, making it visible to students of the
/// target class.
/// </summary>
/// <remarks>
/// Raised by the <c>Assignment</c> aggregate when it transitions to Published. No handler
/// currently consumes this event; it is published so that future side effects (e.g.
/// notifications) can react without the domain depending on any external concern.
/// </remarks>
/// <param name="AssignmentId">The identifier of the published assignment.</param>
/// <param name="PublishedAt">The moment the assignment was published (UTC).</param>
public sealed record AssignmentPublished(Guid AssignmentId, DateTimeOffset PublishedAt) : IDomainEvent;