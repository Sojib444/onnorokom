namespace AssignmentManagement.Application.Common;

/// <summary>
/// Raised when authentication fails (for example invalid credentials at login).
/// Mapped to HTTP 401 by the API's exception middleware.
/// </summary>
public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
