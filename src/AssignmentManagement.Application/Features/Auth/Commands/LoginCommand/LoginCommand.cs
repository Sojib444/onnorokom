using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Auth;

/// <summary>
/// Authenticates a user by email and password and returns a signed access token.
/// Credentials always come from the request body; the resulting identity is encoded
/// only into the token, never trusted from anywhere else.
/// </summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;
