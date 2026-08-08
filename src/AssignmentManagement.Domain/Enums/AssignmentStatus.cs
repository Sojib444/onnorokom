namespace AssignmentManagement.Domain.Enums;

/// <summary>
/// Lifecycle of an assignment. Draft assignments are visible only to their author;
/// published assignments become visible to students of the target class.
/// </summary>
public enum AssignmentStatus
{
    /// <summary>Working copy. Only the author can see it and it can still be edited or deleted.</summary>
    Draft = 0,

    /// <summary>Released to students of the target class. It can no longer be edited or deleted.</summary>
    Published = 1,
}
