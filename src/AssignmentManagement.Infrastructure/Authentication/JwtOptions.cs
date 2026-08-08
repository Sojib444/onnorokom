namespace AssignmentManagement.Infrastructure.Authentication;

/// <summary>
/// JWT configuration, bound from the <c>Jwt</c> section of configuration. The secret
/// must be supplied through configuration or environment variables; a strong, unique
/// value is required in every environment.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>The symmetric signing key. Must be at least 32 bytes (256 bits).</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>The token issuer, included in the token and validated on receipt.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>The token audience, included in the token and validated on receipt.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Access token lifetime in minutes. Defaults to 60.</summary>
    public int ExpiresInMinutes { get; set; } = 60;
}
