namespace AssignmentManagement.Application.Contracts;

/// <summary>Credentials submitted to the login endpoint.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Identity of the authenticated user embedded in a login response.</summary>
public sealed record AuthenticatedUserResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    Guid? ClassId);

/// <summary>Successful login result: a signed access token plus the user's identity.</summary>
public sealed record LoginResponse(
    string Token,
    string TokenType,
    DateTimeOffset ExpiresAt,
    AuthenticatedUserResponse User);
