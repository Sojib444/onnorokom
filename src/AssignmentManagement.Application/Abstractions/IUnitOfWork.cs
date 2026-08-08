namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Coordinates persistence for a single application operation. Command handlers call
/// repository methods and then commit once with <see cref="SaveChangesAsync"/>;
/// repositories never commit on their own. This gives every command an atomic
/// unit of work.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persists all tracked changes made during the operation.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
