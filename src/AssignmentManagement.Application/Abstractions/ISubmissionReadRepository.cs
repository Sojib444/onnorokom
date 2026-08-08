using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Read-side persistence abstraction for <see cref="Submission"/>. Every query runs
/// untracked because results are only projected to DTOs.
/// </summary>
public interface ISubmissionReadRepository
{
    /// <summary>Finds a submission by identifier, with attachments eagerly loaded.</summary>
    Task<Submission?> GetByIdWithAttachmentsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Finds a submission by identifier without navigation data.</summary>
    Task<Submission?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Returns all submissions for an assignment, with attachments; used by the teacher and admin.</summary>
    Task<IReadOnlyList<Submission>> GetByAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken);

    /// <summary>Returns a student's submissions, newest first, for their dashboard.</summary>
    Task<IReadOnlyList<Submission>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken);

    /// <summary>Finds the student's existing submission for an assignment, or null.</summary>
    Task<Submission?> GetByAssignmentAndStudentAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken cancellationToken);

    /// <summary>Whether the student has already submitted an answer for the assignment.</summary>
    Task<bool> ExistsForAssignmentAndStudentAsync(
        Guid assignmentId,
        Guid studentId,
        CancellationToken cancellationToken);

    /// <summary>Whether the student has at least one submission.</summary>
    Task<bool> ExistsForStudentAsync(Guid studentId, CancellationToken cancellationToken);
}
