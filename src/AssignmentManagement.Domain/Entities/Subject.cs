using AssignmentManagement.Domain.Common;
using AssignmentManagement.Domain.Exceptions;

namespace AssignmentManagement.Domain.Entities;

/// <summary>
/// A subject taught at the institution, for example "Mathematics" or "English".
/// A subject is identified by its unique code.
/// </summary>
public sealed class Subject : Entity
{
    private const int MaxNameLength = 100;
    private const int MaxCodeLength = 20;

    /// <summary>Display name of the subject.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Short, unique code, for example "MATH-101".</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Persistence-only constructor for EF Core materialization.</summary>
    private Subject()
    {
    }

    /// <summary>
    /// Creates a subject.
    /// </summary>
    /// <exception cref="BusinessRuleViolation">Thrown when name or code is empty or too long.</exception>
    public Subject(string name, string code)
    {
        Name = Normalize(name, nameof(Name), MaxNameLength);
        Code = Normalize(code, nameof(Code), MaxCodeLength);
    }

    /// <summary>
    /// Updates the name and code.
    /// </summary>
    public void Update(string name, string code)
    {
        Name = Normalize(name, nameof(Name), MaxNameLength);
        Code = Normalize(code, nameof(Code), MaxCodeLength);
    }

    private static string Normalize(string value, string property, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim();

        if (normalized.Length == 0)
        {
            throw new BusinessRuleViolation($"A subject {property.ToLowerInvariant()} is required.");
        }

        if (normalized.Length > maxLength)
        {
            throw new BusinessRuleViolation($"A subject {property.ToLowerInvariant()} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
