using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.TeacherAssignments;

/// <summary>Returns all teacher allocations with resolved names. Administrators only.</summary>
public sealed record GetTeacherAssignmentsQuery : IRequest<IReadOnlyList<TeacherAssignmentResponse>>;
