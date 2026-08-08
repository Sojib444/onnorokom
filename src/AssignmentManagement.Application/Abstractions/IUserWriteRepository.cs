using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Write-side persistence abstraction for <see cref="User"/>. Loaded aggregates stay
/// tracked by the write context so profile and password changes persist with the unit
/// of work.
/// </summary>
public interface IUserWriteRepository
{
    /// <summary>Finds a tracked user by identifier so it can be modified or deleted.</summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Tracks a new user for insertion by the unit of work.</summary>
    void Add(User user);

    /// <summary>Tracks an existing user for update by the unit of work.</summary>
    void Update(User user);

    /// <summary>Tracks a user for deletion by the unit of work.</summary>
    void Remove(User user);
}
