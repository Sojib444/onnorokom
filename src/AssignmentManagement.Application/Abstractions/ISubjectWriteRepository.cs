using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Write-side persistence abstraction for <see cref="Subject"/>. Loaded aggregates stay
/// tracked by the write context so edits persist with the unit of work.
/// </summary>
public interface ISubjectWriteRepository
{
    /// <summary>Finds a tracked subject by identifier so it can be modified or deleted.</summary>
    Task<Subject?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Tracks a new subject for insertion by the unit of work.</summary>
    void Add(Subject entity);

    /// <summary>Tracks an existing subject for update by the unit of work.</summary>
    void Update(Subject entity);

    /// <summary>Tracks a subject for deletion by the unit of work.</summary>
    void Remove(Subject entity);
}
