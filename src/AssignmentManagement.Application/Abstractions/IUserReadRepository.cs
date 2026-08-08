using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Read-side persistence abstraction for <see cref="User"/>. Every query runs untracked
/// because results are only projected to DTOs.
/// </summary>
public interface IUserReadRepository
{
    /// <summary>Finds a user by identifier.</summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Finds a user by their (normalized) email address.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Returns all users, oldest first.</summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Whether another user already uses the given email.</summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Whether any student belongs to the given class.</summary>
    Task<bool> ExistsStudentInClassAsync(Guid classId, CancellationToken cancellationToken);
}
