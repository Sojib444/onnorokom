namespace AssignmentManagement.Application.Contracts;

/// <summary>A user as exposed to the API. Never contains a password or password hash.</summary>
public sealed record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    Guid? ClassId,
    string? ClassName,
    DateTimeOffset CreatedAt);

/// <summary>Body for creating a user. Role is fixed at creation time.</summary>
public sealed record CreateUserRequest(
    string FullName,
    string Email,
    string Password,
    string Role,
    Guid? ClassId);

/// <summary>
/// Body for updating a user's profile. Only the display name and (for students) the
/// class can change; the role and email are immutable once the account exists.
/// </summary>
public sealed record UpdateUserRequest(string FullName, Guid? ClassId);

/// <summary>Body for an administrator resetting a user's password.</summary>
public sealed record UpdatePasswordRequest(string Password);
