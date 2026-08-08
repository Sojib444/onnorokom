namespace AssignmentManagement.Application.Common;

/// <summary>
/// Raised by query and command handlers when a requested resource does not exist.
/// Mapped to HTTP 404 by the API's exception middleware.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a not-found exception for a named resource by identifier.
    /// </summary>
    public static NotFoundException For<TEntity>(Guid id) =>
        new($"{typeof(TEntity).Name} with id '{id}' was not found.");
}
