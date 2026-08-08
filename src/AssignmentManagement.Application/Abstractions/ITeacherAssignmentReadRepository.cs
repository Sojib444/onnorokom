using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Read-side persistence abstraction for <see cref="TeacherAssignment"/>, the allocation
/// that authorizes a teacher to author assignments for a class/subject pair. Every query
/// runs untracked because results are only projected to DTOs.
/// </summary>
public interface ITeacherAssignmentReadRepository
{
    /// <summary>Finds an allocation by identifier.</summary>
    Task<TeacherAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Returns all allocations for a teacher.</summary>
    Task<IReadOnlyList<TeacherAssignment>> GetByTeacherAsync(Guid teacherId, CancellationToken cancellationToken);

    /// <summary>Returns all allocations, teacher first then class, useful for admin views.</summary>
    Task<IReadOnlyList<TeacherAssignment>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Whether the teacher is allocated to the class/subject pair. This is the
    /// authorization check a teacher's assignment commands must pass.
    /// </summary>
    Task<bool> ExistsForTeacherAsync(Guid teacherId, Guid classId, Guid subjectId, CancellationToken cancellationToken);
}
