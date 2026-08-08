using System.Text.RegularExpressions;
using AssignmentManagement.Domain.Common;
using AssignmentManagement.Domain.Exceptions;

namespace AssignmentManagement.Domain.ValueObjects;

/// <summary>
/// An email address. Normalized to lower case and validated with a pragmatic pattern
/// that catches the common mistakes without rejecting legitimate addresses.
/// </summary>
public sealed partial class EmailAddress : ValueObject
{
    private const int MaxLength = 254;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    /// <summary>The normalized (lower-cased) email address.</summary>
    public string Value { get; }

    /// <summary>
    /// Creates a validated email address.
    /// </summary>
    /// <exception cref="BusinessRuleViolation">
    /// Thrown when the value is null, empty, longer than 254 characters or does not match the
    /// email pattern.
    /// </exception>
    public EmailAddress(string value)
    {
        var candidate = (value ?? string.Empty).Trim();

        if (candidate.Length == 0)
        {
            throw new BusinessRuleViolation("An email address is required.");
        }

        if (candidate.Length > MaxLength)
        {
            throw new BusinessRuleViolation($"An email address cannot exceed {MaxLength} characters.");
        }

        if (!EmailPattern().IsMatch(candidate))
        {
            throw new BusinessRuleViolation("The email address is not valid.");
        }

        Value = candidate.ToLowerInvariant();
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
