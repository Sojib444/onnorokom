using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Write-side persistence abstraction for <see cref="Class"/>. Loaded aggregates stay
/// tracked by the write context so edits persist with the unit of work.
/// </summary>
public interface IClassWriteRepository
{
    /// <summary>Finds a tracked class by identifier so it can be modified or deleted.</summary>
    Task<Class?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Tracks a new class for insertion by the unit of work.</summary>
    void Add(Class entity);

    /// <summary>Tracks an existing class for update by the unit of work.</summary>
    void Update(Class entity);

    /// <summary>Tracks a class for deletion by the unit of work.</summary>
    void Remove(Class entity);
}
