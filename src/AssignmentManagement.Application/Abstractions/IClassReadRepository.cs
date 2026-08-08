using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Read-side persistence abstraction for <see cref="Class"/>. Every query runs
/// untracked because results are only projected to DTOs.
/// </summary>
public interface IClassReadRepository
{
    /// <summary>Finds a class by identifier.</summary>
    Task<Class?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Returns all classes ordered by name.</summary>
    Task<IReadOnlyList<Class>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Whether a class with the given identifier exists.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Whether any class already uses the given name.</summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
}
