using AssignmentManagement.Domain.Common;
using AssignmentManagement.Domain.Exceptions;

namespace AssignmentManagement.Domain.Entities;

/// <summary>
/// A class (or course) that students belong to and that assignments are targeted at.
/// A class has no identity beyond its name — there are no sibling sections.
/// </summary>
public sealed class Class : Entity
{
    private const int MaxNameLength = 100;

    /// <summary>Name of the class, for example "Grade 7" or "CSE-2026".</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Optional description of the class.</summary>
    public string? Description { get; private set; }

    /// <summary>Persistence-only constructor for EF Core materialization.</summary>
    private Class()
    {
    }

    /// <summary>
    /// Creates a class.
    /// </summary>
    /// <exception cref="BusinessRuleViolation">Thrown when the name is empty or too long.</exception>
    public Class(string name, string? description)
    {
        Name = NormalizeName(name);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Updates the name and description.
    /// </summary>
    public void Update(string name, string? description)
    {
        Name = NormalizeName(name);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private static string NormalizeName(string name)
    {
        var normalized = (name ?? string.Empty).Trim();

        if (normalized.Length == 0)
        {
            throw new BusinessRuleViolation("A class name is required.");
        }

        if (normalized.Length > MaxNameLength)
        {
            throw new BusinessRuleViolation($"A class name cannot exceed {MaxNameLength} characters.");
        }

        return normalized;
    }
}
