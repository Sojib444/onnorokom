using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Provides the authenticated caller's identity. Values are always derived from the
/// validated JWT by the infrastructure implementation — never from the request body.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated user's identifier, or null when anonymous.</summary>
    Guid? UserId { get; }

    /// <summary>The authenticated user's email, or null when anonymous.</summary>
    string? Email { get; }

    /// <summary>The authenticated user's role, or null when anonymous.</summary>
    UserRole? Role { get; }

    /// <summary>The class of an authenticated student, or null otherwise.</summary>
    Guid? ClassId { get; }

    /// <summary>Whether the request carries a valid authenticated identity.</summary>
    bool IsAuthenticated { get; }
}
