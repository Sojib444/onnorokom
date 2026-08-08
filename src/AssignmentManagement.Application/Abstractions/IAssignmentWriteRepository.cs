using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Write-side persistence abstraction for <see cref="Assignment"/>. Loaded aggregates
/// stay tracked by the write context so changes persist with the unit of work.
/// </summary>
public interface IAssignmentWriteRepository
{
    /// <summary>Finds a tracked assignment by identifier so it can be modified or deleted.</summary>
    Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Tracks a new assignment for insertion by the unit of work.</summary>
    void Add(Assignment entity);

    /// <summary>Tracks an existing assignment for update by the unit of work.</summary>
    void Update(Assignment entity);

    /// <summary>Tracks an assignment for deletion by the unit of work.</summary>
    void Remove(Assignment entity);
}
