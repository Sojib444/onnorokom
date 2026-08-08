using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Assignments;

/// <summary>
/// Returns the assignments visible to the caller: all for administrators, the caller's
/// own for teachers, and published assignments for the student's class.
/// </summary>
public sealed record GetAssignmentsQuery : IRequest<IReadOnlyList<AssignmentResponse>>;
