using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Abstractions;

/// <summary>A signed access token and its absolute expiry time.</summary>
public sealed record TokenResult(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// Issues signed JWT access tokens for authenticated users. Implemented by the
/// infrastructure layer; the application only ever depends on this abstraction.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Creates a signed access token carrying the user's identity and role.</summary>
    TokenResult GenerateToken(User user);
}
