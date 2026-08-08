using AssignmentManagement.Domain.Common;

namespace AssignmentManagement.Domain.Events;

/// <summary>
/// Raised when a teacher awards marks and feedback to a submission.
/// </summary>
/// <remarks>
/// Raised by the <c>Submission</c> aggregate when it is graded. No handler currently
/// consumes this event; it exists so future side effects (e.g. notifying the student)
/// do not require the domain to reference any external concern.
/// </remarks>
/// <param name="SubmissionId">The submission that was graded.</param>
/// <param name="Marks">The awarded mark.</param>
/// <param name="Feedback">The teacher's feedback, if any.</param>
public sealed record SubmissionGraded(Guid SubmissionId, decimal Marks, string? Feedback) : IDomainEvent;
