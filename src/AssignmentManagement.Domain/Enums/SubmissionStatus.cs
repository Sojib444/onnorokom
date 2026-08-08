namespace AssignmentManagement.Domain.Enums;

/// <summary>
/// Lifecycle of a submission. The transitions are enforced by <see cref="Entities.Submission"/>.
/// </summary>
public enum SubmissionStatus
{
    /// <summary>Submitted by the student and awaiting grading. Can still be updated before the deadline.</summary>
    Submitted = 0,

    /// <summary>Returned by the teacher for revision. The student may edit even after the deadline.</summary>
    Returned = 1,

    /// <summary>Graded: marks and feedback have been awarded by the teacher.</summary>
    Graded = 2,
}
