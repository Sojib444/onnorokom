using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Users;

/// <summary>Returns all users, oldest first. Administrators only.</summary>
public sealed record GetUsersQuery : IRequest<IReadOnlyList<UserResponse>>;
