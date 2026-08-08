using MediatR;

namespace AssignmentManagement.Application.Features.TeacherAssignments;

/// <summary>Removes a teacher allocation. Administrators only.</summary>
public sealed record DeleteTeacherAssignmentCommand(Guid Id) : IRequest<Unit>;
