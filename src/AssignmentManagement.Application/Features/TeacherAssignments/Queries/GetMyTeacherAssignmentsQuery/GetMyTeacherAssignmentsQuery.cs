using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.TeacherAssignments;

/// <summary>
/// Returns the authenticated teacher's own allocations with resolved names. Powers the
/// assignment authoring form, which only offers class/subject pairs the teacher may use.
/// </summary>
public sealed record GetMyTeacherAssignmentsQuery : IRequest<IReadOnlyList<TeacherAssignmentResponse>>;
