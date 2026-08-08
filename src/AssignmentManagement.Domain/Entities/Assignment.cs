using AssignmentManagement.Domain.Common;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.Events;
using AssignmentManagement.Domain.Exceptions;

namespace AssignmentManagement.Domain.Entities;

/// <summary>
/// An assignment set by a teacher for one class and one subject, with a title,
/// description, deadline and maximum marks. The assignment is the aggregate root of
/// the Assignment aggregate.
/// </summary>
/// <remarks>
/// Invariants:
/// <list type="bullet">
/// <item>Title is required and at most 200 characters.</item>
/// <item>Description is required.</item>
/// <item>Maximum marks must be greater than zero.</item>
/// <item>The deadline must lie in the future at creation and at publish time.</item>
/// <item>An assignment must reference exactly one teacher, one class and one subject.</item>
/// <item>Only draft assignments can be edited or deleted.</item>
/// <item>Only draft assignments can be published; publishing is permanent.</item>
/// <item>A published assignment can no longer be changed.</item>
/// </list>
/// All business rules above are enforced here so that no controller or handler can
/// accidentally bypass them.
/// </remarks>
public sealed class Assignment : AggregateRoot, IAuditable
{
    private const int MaxTitleLength = 200;

    /// <summary>The teacher who authored this assignment.</summary>
    public Guid TeacherId { get; private set; }

    /// <summary>The class the assignment is targeted at.</summary>
    public Guid ClassId { get; private set; }

    /// <summary>The subject the assignment belongs to.</summary>
    public Guid SubjectId { get; private set; }

    /// <summary>
    /// Navigation to the target class. Read-only reference to another aggregate; loaded
    /// lazily or eagerly by the repository only when a view needs the class name.
    /// </summary>
    public Class? Class { get; private set; }

    /// <summary>
    /// Navigation to the subject. Read-only reference to another aggregate; loaded
    /// lazily or eagerly by the repository only when a view needs the subject name.
    /// </summary>
    public Subject? Subject { get; private set; }

    /// <summary>Short title of the assignment.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Full description of what the students must do.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>UTC deadline after which submissions are closed, unless reopened.</summary>
    public DateTimeOffset Deadline { get; private set; }

    /// <summary>The highest mark a submission to this assignment can earn.</summary>
    public decimal MaximumMarks { get; private set; }

    /// <summary>Draft (invisible to students) or Published (visible and submittable).</summary>
    public AssignmentStatus Status { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Persistence-only constructor for EF Core materialization.</summary>
    private Assignment()
    {
    }

    /// <summary>
    /// Creates a draft assignment.
    /// </summary>
    /// <exception cref="BusinessRuleViolation">
    /// Thrown when an identifier is empty, the title or description is missing, the
    /// maximum marks are not positive, or the deadline is not in the future.
    /// </exception>
    public Assignment(
        Guid teacherId,
        Guid classId,
        Guid subjectId,
        string title,
        string description,
        DateTimeOffset deadline,
        decimal maximumMarks,
        DateTimeOffset now)
    {
        TeacherId = Require(teacherId, nameof(TeacherId));
        ClassId = Require(classId, nameof(ClassId));
        SubjectId = Require(subjectId, nameof(SubjectId));
        Title = NormalizeTitle(title);
        Description = NormalizeDescription(description);
        Deadline = ValidateDeadline(deadline, now);
        MaximumMarks = ValidateMaximumMarks(maximumMarks);
        Status = AssignmentStatus.Draft;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Publishes the assignment, making it visible and submittable for students of the
    /// target class.
    /// </summary>
    /// <exception cref="InvalidStateTransition">Thrown when the assignment is not in Draft status.</exception>
    /// <exception cref="BusinessRuleViolation">Thrown when the deadline has already passed.</exception>
    public void Publish(DateTimeOffset now)
    {
        if (Status != AssignmentStatus.Draft)
        {
            throw new InvalidStateTransition("Only draft assignments can be published.");
        }

        if (Deadline <= now)
        {
            throw new BusinessRuleViolation("An assignment cannot be published after its deadline has passed.");
        }

        Status = AssignmentStatus.Published;
        UpdatedAt = now;

        RaiseDomainEvent(new AssignmentPublished(Id, now));
    }

    /// <summary>
    /// Replaces the assignment's content. Only possible while the assignment is still a
    /// draft; a published assignment is immutable.
    /// </summary>
    /// <exception cref="InvalidStateTransition">Thrown when the assignment is not in Draft status.</exception>
    public void Update(
        string title,
        string description,
        DateTimeOffset deadline,
        decimal maximumMarks,
        Guid classId,
        Guid subjectId,
        DateTimeOffset now)
    {
        if (Status != AssignmentStatus.Draft)
        {
            throw new InvalidStateTransition("Only draft assignments can be edited.");
        }

        Title = NormalizeTitle(title);
        Description = NormalizeDescription(description);
        Deadline = ValidateDeadline(deadline, now);
        MaximumMarks = ValidateMaximumMarks(maximumMarks);
        ClassId = Require(classId, nameof(ClassId));
        SubjectId = Require(subjectId, nameof(SubjectId));
        UpdatedAt = now;
    }

    /// <summary>
    /// Verifies the assignment may be deleted. Only drafts can be deleted, mirroring the
    /// rule that a published assignment is immutable.
    /// </summary>
    /// <exception cref="InvalidStateTransition">Thrown when the assignment is published.</exception>
    public void EnsureCanBeDeleted()
    {
        if (Status != AssignmentStatus.Draft)
        {
            throw new InvalidStateTransition("Only draft assignments can be deleted.");
        }
    }

    /// <summary>
    /// Whether students may currently submit answers: the assignment is published and
    /// the deadline has not passed.
    /// </summary>
    public bool IsOpenForSubmission(DateTimeOffset now) =>
        Status == AssignmentStatus.Published && now <= Deadline;

    private static Guid Require(Guid id, string property)
    {
        if (id == Guid.Empty)
        {
            throw new BusinessRuleViolation($"{property} is required.");
        }

        return id;
    }

    private static string NormalizeTitle(string title)
    {
        var normalized = (title ?? string.Empty).Trim();

        if (normalized.Length == 0)
        {
            throw new BusinessRuleViolation("A title is required.");
        }

        if (normalized.Length > MaxTitleLength)
        {
            throw new BusinessRuleViolation($"A title cannot exceed {MaxTitleLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeDescription(string description)
    {
        var normalized = (description ?? string.Empty).Trim();

        if (normalized.Length == 0)
        {
            throw new BusinessRuleViolation("A description is required.");
        }

        return normalized;
    }

    private static DateTimeOffset ValidateDeadline(DateTimeOffset deadline, DateTimeOffset now)
    {
        if (deadline <= now)
        {
            throw new BusinessRuleViolation("The deadline must be in the future.");
        }

        return deadline;
    }

    private static decimal ValidateMaximumMarks(decimal maximumMarks)
    {
        if (maximumMarks <= 0)
        {
            throw new BusinessRuleViolation("Maximum marks must be greater than zero.");
        }

        return maximumMarks;
    }
}
