namespace AssignmentManagement.Domain.Common;

/// <summary>
/// Base class for value objects. A value object has no identity: two value objects of
/// the same type are equal when all of their components are equal.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// The components that participate in equality. All of them must be equal for two
    /// value objects of the same type to be considered equal.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public bool Equals(ValueObject? other) =>
        other is not null &&
        other.GetType() == GetType() &&
        GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ValueObject);

    /// <inheritdoc />
    public override int GetHashCode() =>
        GetEqualityComponents()
            .Select(component => component?.GetHashCode() ?? 0)
            .Aggregate(17, (hash, next) => unchecked((hash * 31) + next));

    /// <summary>Compares two value objects for equality.</summary>
    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Compares two value objects for inequality.</summary>
    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
