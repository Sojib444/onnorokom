namespace AssignmentManagement.Domain.Common;

/// <summary>
/// Marks an entity as carrying creation and modification timestamps. Timestamps are
/// maintained by the persistence layer; callers should treat them as read-only.
/// </summary>
public interface IAuditable
{
    /// <summary>When the record was first persisted.</summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>When the record was last modified.</summary>
    DateTimeOffset UpdatedAt { get; }
}
