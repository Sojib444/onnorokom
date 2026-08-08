using MediatR;

namespace AssignmentManagement.Application.Features.Users;

/// <summary>
/// Deletes a user. A teacher with assignments or a student with submissions cannot be
/// deleted because their historical records must remain intact. Administrators only.
/// </summary>
public sealed record DeleteUserCommand(Guid Id) : IRequest<Unit>;
