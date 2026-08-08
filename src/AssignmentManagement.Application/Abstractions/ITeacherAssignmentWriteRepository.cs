using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Write-side persistence abstraction for <see cref="TeacherAssignment"/>. Loaded
/// aggregates stay tracked by the write context so deletions persist with the unit
/// of work.
/// </summary>
public interface ITeacherAssignmentWriteRepository
{
    /// <summary>Finds a tracked allocation by identifier so it can be deleted.</summary>
    Task<TeacherAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Tracks a new allocation for insertion by the unit of work.</summary>
    void Add(TeacherAssignment entity);

    /// <summary>Tracks an allocation for deletion by the unit of work.</summary>
    void Remove(TeacherAssignment entity);
}
