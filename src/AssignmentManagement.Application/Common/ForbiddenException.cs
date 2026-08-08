namespace AssignmentManagement.Application.Common;

/// <summary>
/// Raised when the caller is authenticated but not allowed to act on a resource,
/// for example a teacher editing another teacher's assignment. Mapped to HTTP 403 by
/// the API's exception middleware.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
