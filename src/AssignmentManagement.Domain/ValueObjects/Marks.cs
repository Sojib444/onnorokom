using AssignmentManagement.Domain.Common;
using AssignmentManagement.Domain.Exceptions;

namespace AssignmentManagement.Domain.ValueObjects;

/// <summary>
/// A non-negative mark awarded to a submission, bounded by the assignment's maximum
/// marks. The ceiling is passed in as a parameter because the grading rule crosses two
/// aggregates and the submission must not hold a reference to the assignment.
/// </summary>
public sealed class Marks : ValueObject
{
    /// <summary>The awarded mark.</summary>
    public decimal Value { get; }

    /// <summary>The assignment's maximum marks that this mark must respect.</summary>
    public decimal Maximum { get; }

    /// <summary>
    /// Creates a validated mark.
    /// </summary>
    /// <exception cref="BusinessRuleViolation">
    /// Thrown when the mark is negative or exceeds <paramref name="maximum"/>.
    /// </exception>
    public Marks(decimal value, decimal maximum)
    {
        if (value < 0)
        {
            throw new BusinessRuleViolation("Marks cannot be negative.");
        }

        if (value > maximum)
        {
            throw new BusinessRuleViolation($"Marks cannot exceed the assignment's maximum of {maximum}.");
        }

        Value = value;
        Maximum = maximum;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return Maximum;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
