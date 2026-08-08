using AssignmentManagement.Domain.Common;

namespace AssignmentManagement.Domain.Events;

/// <summary>
/// Raised when a student submits an answer for an assignment.
/// </summary>
/// <remarks>
/// Raised by the <c>Submission</c> aggregate when a new submission is created. No handler
/// currently consumes this event; it exists so future side effects (e.g. notifying the
/// assignment's teacher) do not require the domain to reference any external concern.
/// </remarks>
/// <param name="SubmissionId">The identifier of the new submission.</param>
/// <param name="AssignmentId">The assignment the submission belongs to.</param>
/// <param name="StudentId">The submitting student.</param>
/// <param name="SubmittedAt">The moment the submission was created (UTC).</param>
public sealed record SubmissionCreated(
    Guid SubmissionId,
    Guid AssignmentId,
    Guid StudentId,
    DateTimeOffset SubmittedAt) : IDomainEvent;
