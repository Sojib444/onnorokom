namespace AssignmentManagement.Domain.Exceptions;

/// <summary>
/// Raised when an operation would move an entity through an invalid lifecycle
/// transition, for example publishing an already published assignment.
/// </summary>
public sealed class InvalidStateTransition : DomainException
{
    /// <inheritdoc />
    public InvalidStateTransition(string message) : base(message)
    {
    }
}
