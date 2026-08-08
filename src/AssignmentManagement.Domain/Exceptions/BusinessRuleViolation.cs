namespace AssignmentManagement.Domain.Exceptions;

/// <summary>
/// Raised when an operation would violate a business rule, for example submitting an
/// answer after the deadline or grading beyond the assignment's maximum marks.
/// </summary>
public sealed class BusinessRuleViolation : DomainException
{
    /// <inheritdoc />
    public BusinessRuleViolation(string message) : base(message)
    {
    }
}
