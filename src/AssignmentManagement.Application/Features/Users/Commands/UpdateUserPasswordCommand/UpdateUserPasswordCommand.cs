using MediatR;

namespace AssignmentManagement.Application.Features.Users;

/// <summary>Resets a user's password. Administrators only.</summary>
public sealed record UpdateUserPasswordCommand(Guid Id, string Password) : IRequest<Unit>;
