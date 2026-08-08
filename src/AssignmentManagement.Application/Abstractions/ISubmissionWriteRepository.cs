using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Write-side persistence abstraction for <see cref="Submission"/>. Loaded submissions
/// (with their attachments) stay tracked so grading, returning and answer edits persist
/// with the unit of work.
/// </summary>
public interface ISubmissionWriteRepository
{
    /// <summary>Finds a tracked submission with its attachments so it can be modified.</summary>
    Task<Submission?> GetByIdWithAttachmentsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Tracks a new submission for insertion by the unit of work.</summary>
    void Add(Submission entity);

    /// <summary>Tracks an existing submission for update by the unit of work.</summary>
    void Update(Submission entity);
}
