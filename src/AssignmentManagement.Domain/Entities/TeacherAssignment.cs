using AssignmentManagement.Domain.Common;
using AssignmentManagement.Domain.Exceptions;

namespace AssignmentManagement.Domain.Entities;

/// <summary>
/// The allocation of a teacher to a class and subject pair. A teacher may only author
/// assignments for pairs they have been allocated to; the pair is unique per teacher.
/// </summary>
/// <remarks>
/// This entity is what makes the ownership rule possible: when a teacher creates or
/// edits an assignment, the application layer verifies that a
/// <see cref="TeacherAssignment"/> exists for the teacher, class and subject.
/// </remarks>
public sealed class TeacherAssignment : Entity
{
    /// <summary>The allocated teacher.</summary>
    public Guid TeacherId { get; private set; }

    /// <summary>The class the teacher is allowed to teach.</summary>
    public Guid ClassId { get; private set; }

    /// <summary>The subject the teacher is allowed to teach.</summary>
    public Guid SubjectId { get; private set; }

    /// <summary>Persistence-only constructor for EF Core materialization.</summary>
    private TeacherAssignment()
    {
    }

    /// <summary>
    /// Creates a teacher allocation.
    /// </summary>
    /// <exception cref="BusinessRuleViolation">Thrown when any identifier is empty.</exception>
    public TeacherAssignment(Guid teacherId, Guid classId, Guid subjectId)
    {
        TeacherId = Require(teacherId, nameof(TeacherId));
        ClassId = Require(classId, nameof(ClassId));
        SubjectId = Require(subjectId, nameof(SubjectId));
    }

    private static Guid Require(Guid id, string property)
    {
        if (id == Guid.Empty)
        {
            throw new BusinessRuleViolation($"{property} is required.");
        }

        return id;
    }
}
