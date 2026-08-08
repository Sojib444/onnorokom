using AssignmentManagement.Domain.Common;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.Events;
using AssignmentManagement.Domain.Exceptions;
using AssignmentManagement.Domain.ValueObjects;

namespace AssignmentManagement.Domain.Entities;

/// <summary>
/// A student's answer to an assignment. Exactly one submission exists per student per
/// assignment; revising edits the existing record. The submission is the aggregate root
/// of the Submission aggregate and owns its attachments.
/// </summary>
/// <remarks>
/// Invariants:
/// <list type="bullet">
/// <item>An answer is required on every submit or update.</item>
/// <item>Submission requires the assignment to be published.</item>
/// <item>Submission is closed after the deadline — with one explicit exception: a
/// submission returned for revision can be edited past the deadline.</item>
/// <item>Only the teacher who authored the assignment can grade or return a submission.</item>
/// <item>Marks are bounded by the assignment's maximum marks (see <see cref="Marks"/>).</item>
/// <item>Marking is manual and per submission; there is no bulk grading.</item>
/// </list>
/// Because the submission does not hold a reference to the assignment (a different
/// aggregate), the few rules that need assignment data — whether it is published, its
/// deadline, its maximum marks — are passed in as parameters. This keeps the grading
/// ceiling in the domain while staying testable without a database.
/// </remarks>
public sealed class Submission : AggregateRoot, IAuditable
{
    /// <summary>The assignment this submission answers.</summary>
    public Guid AssignmentId { get; private set; }

    /// <summary>The student who owns this submission.</summary>
    public Guid StudentId { get; private set; }

    /// <summary>The submitted answer text.</summary>
    public string Answer { get; private set; } = string.Empty;

    /// <summary>Current lifecycle state, enforced by the transitions below.</summary>
    public SubmissionStatus Status { get; private set; }

    /// <summary>The awarded mark; null until the submission is graded.</summary>
    public decimal? Marks { get; private set; }

    /// <summary>The teacher's feedback; null until the submission is graded.</summary>
    public string? Feedback { get; private set; }

    /// <summary>When the current version of the answer was last submitted (UTC).</summary>
    public DateTimeOffset? SubmittedAt { get; private set; }

    /// <summary>When marks were last awarded (UTC); null until graded.</summary>
    public DateTimeOffset? GradedAt { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<SubmissionAttachment> _attachments = [];

    /// <summary>Files attached to this submission.</summary>
    public IReadOnlyCollection<SubmissionAttachment> Attachments => _attachments;

    /// <summary>Persistence-only constructor for EF Core materialization.</summary>
    private Submission()
    {
    }

    /// <summary>
    /// Creates a submission for a published assignment whose deadline has not passed.
    /// </summary>
    /// <param name="assignmentId">The assignment being answered.</param>
    /// <param name="studentId">The submitting student.</param>
    /// <param name="answer">The answer text.</param>
    /// <param name="assignmentPublished">Whether the assignment is published.</param>
    /// <param name="assignmentDeadline">The assignment's UTC deadline.</param>
    /// <param name="now">Current UTC timestamp.</param>
    /// <exception cref="BusinessRuleViolation">
    /// Thrown when the answer is empty, the assignment is not published, or the
    /// deadline has passed.
    /// </exception>
    public static Submission Create(
        Guid assignmentId,
        Guid studentId,
        string answer,
        bool assignmentPublished,
        DateTimeOffset assignmentDeadline,
        DateTimeOffset now)
    {
        if (!assignmentPublished)
        {
            throw new BusinessRuleViolation("The assignment must be published before students can submit.");
        }

        if (now > assignmentDeadline)
        {
            throw new BusinessRuleViolation("The assignment deadline has passed; submissions are closed.");
        }

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = studentId,
            Answer = NormalizeAnswer(answer),
            Status = SubmissionStatus.Submitted,
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        submission.RaiseDomainEvent(new SubmissionCreated(submission.Id, assignmentId, studentId, now));
        return submission;
    }

    /// <summary>
    /// Replaces the answer text. Allowed while the submission is still pending
    /// (Submitted and before the deadline), or whenever the teacher has returned it for
    /// revision — the reopened state is the one auditable exception to the deadline rule.
    /// Updating a returned submission resubmits it, moving it back to Submitted.
    /// </summary>
    /// <exception cref="InvalidStateTransition">Thrown when the submission is already graded.</exception>
    /// <exception cref="BusinessRuleViolation">Thrown when the answer is empty or the deadline has passed while the submission was pending.</exception>
    public void UpdateAnswer(string answer, DateTimeOffset assignmentDeadline, DateTimeOffset now)
    {
        if (Status == SubmissionStatus.Graded)
        {
            throw new InvalidStateTransition("A graded submission cannot be edited.");
        }

        if (Status == SubmissionStatus.Submitted && now > assignmentDeadline)
        {
            throw new BusinessRuleViolation("The assignment deadline has passed; submissions are closed.");
        }

        Answer = NormalizeAnswer(answer);
        SubmittedAt = now;

        if (Status == SubmissionStatus.Returned)
        {
            Status = SubmissionStatus.Submitted;
        }

        UpdatedAt = now;
    }

    /// <summary>
    /// Returns the submission to the student for revision. Clears any marks and
    /// feedback. While returned, the student may edit the submission even past the
    /// deadline; the returned status is the recorded audit trail for that exception.
    /// </summary>
    /// <exception cref="BusinessRuleViolation">
    /// Thrown when the caller is not the assignment's teacher.
    /// </exception>
    public void ReturnForRevision(Guid teacherId, Guid assignmentTeacherId, DateTimeOffset now)
    {
        EnsureTeacherOwnership(teacherId, assignmentTeacherId);

        Status = SubmissionStatus.Returned;
        Marks = null;
        Feedback = null;
        GradedAt = null;
        UpdatedAt = now;
    }

    /// <summary>
    /// Grades the submission with marks and feedback. The marks must respect the
    /// assignment's maximum marks, which is passed in as a parameter to keep the rule
    /// inside the domain without coupling the two aggregates.
    /// </summary>
    /// <exception cref="BusinessRuleViolation">
    /// Thrown when the caller is not the assignment's teacher, or when the marks are
    /// negative or exceed the maximum.
    /// </exception>
    public void Grade(
        Guid teacherId,
        Guid assignmentTeacherId,
        decimal maximumMarks,
        decimal marks,
        string? feedback,
        DateTimeOffset now)
    {
        EnsureTeacherOwnership(teacherId, assignmentTeacherId);

        var validatedMarks = new Marks(marks, maximumMarks);

        Marks = validatedMarks.Value;
        Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
        Status = SubmissionStatus.Graded;
        GradedAt = now;
        UpdatedAt = now;

        RaiseDomainEvent(new SubmissionGraded(Id, validatedMarks.Value, Feedback));
    }

    /// <summary>
    /// Attaches a file to the submission.
    /// </summary>
    public void AddAttachment(SubmissionAttachment attachment)
    {
        _attachments.Add(attachment);
    }

    /// <summary>
    /// Removes all attachments so they can be replaced. The files themselves are deleted
    /// from storage by the caller.
    /// </summary>
    public void ClearAttachments()
    {
        _attachments.Clear();
    }

    private static void EnsureTeacherOwnership(Guid teacherId, Guid assignmentTeacherId)
    {
        if (teacherId != assignmentTeacherId)
        {
            throw new BusinessRuleViolation("Only the assignment's teacher can grade or return this submission.");
        }
    }

    private static string NormalizeAnswer(string answer)
    {
        var normalized = (answer ?? string.Empty).Trim();

        if (normalized.Length == 0)
        {
            throw new BusinessRuleViolation("An answer is required.");
        }

        return normalized;
    }
}
