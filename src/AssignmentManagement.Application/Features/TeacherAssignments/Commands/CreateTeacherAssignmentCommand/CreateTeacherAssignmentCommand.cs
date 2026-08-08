using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.TeacherAssignments;

/// <summary>Allocates a teacher to a class and subject. Administrators only.</summary>
public sealed record CreateTeacherAssignmentCommand(
    Guid TeacherId,
    Guid ClassId,
    Guid SubjectId) : IRequest<TeacherAssignmentResponse>;
