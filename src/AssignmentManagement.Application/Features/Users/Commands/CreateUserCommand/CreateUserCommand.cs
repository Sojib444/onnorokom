using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Users;

/// <summary>Creates a user account with a password. Administrators only.</summary>
public sealed record CreateUserCommand(
    string FullName,
    string Email,
    string Password,
    string Role,
    Guid? ClassId) : IRequest<UserResponse>;
