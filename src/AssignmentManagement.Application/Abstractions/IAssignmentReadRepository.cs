using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Read-side persistence abstraction for <see cref="Assignment"/>. Every query runs
/// untracked (no change tracking)
/// because results are only projected to DTOs; nothing here mutates state.
/// </summary>
public interface IAssignmentReadRepository
{
    /// <summary>Finds an assignment by identifier, with its class and subject eagerly loaded.</summary>
    Task<Assignment?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Finds an assignment by identifier without navigation data.</summary>
    Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Returns all assignments, newest first; used by administrators.</summary>
    Task<IReadOnlyList<Assignment>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Returns a teacher's assignments, newest first, for their dashboard.</summary>
    Task<IReadOnlyList<Assignment>> GetByTeacherAsync(Guid teacherId, CancellationToken cancellationToken);

    /// <summary>Returns assignments the teacher authored for a specific class.</summary>
    Task<IReadOnlyList<Assignment>> GetByTeacherAndClassAsync(
        Guid teacherId,
        Guid classId,
        CancellationToken cancellationToken);

    /// <summary>Returns published assignments targeted at a student's class, newest first.</summary>
    Task<IReadOnlyList<Assignment>> GetPublishedForClassAsync(Guid classId, CancellationToken cancellationToken);

    /// <summary>Whether the teacher has authored at least one assignment.</summary>
    Task<bool> ExistsForTeacherAsync(Guid teacherId, CancellationToken cancellationToken);

    /// <summary>Whether any assignment targets the given class.</summary>
    Task<bool> ExistsForClassAsync(Guid classId, CancellationToken cancellationToken);

    /// <summary>Whether any assignment uses the given subject.</summary>
    Task<bool> ExistsForSubjectAsync(Guid subjectId, CancellationToken cancellationToken);
}
