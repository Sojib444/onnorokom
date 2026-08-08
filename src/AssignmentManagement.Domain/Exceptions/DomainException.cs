namespace AssignmentManagement.Domain.Exceptions;

/// <summary>
/// Base class for all exceptions raised by the domain layer. Catching this type in the
/// application layer guarantees that business rules are enforced independently of any
/// storage or HTTP concern.
/// </summary>
public abstract class DomainException : Exception
{
    /// <inheritdoc />
    protected DomainException(string message) : base(message)
    {
    }
}
