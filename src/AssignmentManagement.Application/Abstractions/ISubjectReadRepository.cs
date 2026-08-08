using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Read-side persistence abstraction for <see cref="Subject"/>. Every query runs
/// untracked because results are only projected to DTOs.
/// </summary>
public interface ISubjectReadRepository
{
    /// <summary>Finds a subject by identifier.</summary>
    Task<Subject?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Returns all subjects ordered by name.</summary>
    Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Whether a subject with the given code already exists.</summary>
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>Whether a subject with the given identifier exists.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
