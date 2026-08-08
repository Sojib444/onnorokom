using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Users;

/// <summary>Returns a single user by identifier. Administrators only.</summary>
public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserResponse>;
