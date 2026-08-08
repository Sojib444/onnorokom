using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Users;

/// <summary>
/// Updates a user's display name and, for students, their class. The role and email are
/// immutable after creation. Administrators only.
/// </summary>
public sealed record UpdateUserCommand(Guid Id, string FullName, Guid? ClassId) : IRequest<UserResponse>;
