namespace AssignmentManagement.Application.Contracts;

/// <summary>
/// The allocation of a teacher to a class and subject pair, with the names resolved for
/// display. The pair authorizes the teacher to author assignments for it.
/// </summary>
public sealed record TeacherAssignmentResponse(
    Guid Id,
    Guid TeacherId,
    string TeacherName,
    Guid ClassId,
    string ClassName,
    Guid SubjectId,
    string SubjectName);

/// <summary>Body for allocating a teacher to a class and subject.</summary>
public sealed record CreateTeacherAssignmentRequest(Guid TeacherId, Guid ClassId, Guid SubjectId);
