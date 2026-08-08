using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.Events;
using AssignmentManagement.Domain.Exceptions;
using FluentAssertions;

namespace AssignmentManagement.UnitTests.Domain;

public sealed class SubmissionTests
{
    private static readonly Guid AssignmentId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly DateTimeOffset Deadline = new(2026, 8, 10, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    private static Submission CreateSubmission(string answer = "My answer.") =>
        Submission.Create(AssignmentId, StudentId, answer, assignmentPublished: true, Deadline, Now);

    [Fact]
    public void Create_WhenPublishedAndBeforeDeadline_CreatesSubmittedSubmission()
    {
        var submission = CreateSubmission();

        submission.Status.Should().Be(SubmissionStatus.Submitted);
        submission.StudentId.Should().Be(StudentId);
        submission.AssignmentId.Should().Be(AssignmentId);
        submission.Answer.Should().Be("My answer.");
        submission.Marks.Should().BeNull();
        submission.SubmittedAt.Should().Be(Now);
        submission.DomainEvents.OfType<SubmissionCreated>().Should().ContainSingle().Which.Should()
            .Match<SubmissionCreated>(created =>
                created.SubmissionId == submission.Id &&
                created.AssignmentId == AssignmentId &&
                created.StudentId == StudentId);
    }

    [Fact]
    public void Create_WhenAssignmentNotPublished_Throws()
    {
        var act = () => Submission.Create(AssignmentId, StudentId, "My answer.",
            assignmentPublished: false, Deadline, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*published*");
    }

    [Fact]
    public void Create_WhenDeadlinePassed_Throws()
    {
        var act = () => Submission.Create(AssignmentId, StudentId, "My answer.",
            assignmentPublished: true, Deadline, Deadline.AddMinutes(1));

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*deadline*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyAnswer_Throws(string? answer)
    {
        var act = () => Submission.Create(AssignmentId, StudentId, answer!,
            assignmentPublished: true, Deadline, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*answer*");
    }

    [Fact]
    public void UpdateAnswer_BeforeDeadline_UpdatesAndResubmits()
    {
        var submission = CreateSubmission();
        var beforeDeadline = Deadline.AddMinutes(-10);

        submission.UpdateAnswer("Revised answer.", Deadline, beforeDeadline);

        submission.Answer.Should().Be("Revised answer.");
        submission.Status.Should().Be(SubmissionStatus.Submitted);
        submission.SubmittedAt.Should().Be(beforeDeadline);
    }

    [Fact]
    public void UpdateAnswer_AfterDeadline_WhenPending_Throws()
    {
        var submission = CreateSubmission();

        var act = () => submission.UpdateAnswer("Revised answer.", Deadline, Deadline.AddMinutes(1));

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*deadline*");
    }

    [Fact]
    public void UpdateAnswer_AfterDeadline_WhenReturnedForRevision_IsAllowed()
    {
        var submission = CreateSubmission();
        submission.ReturnForRevision(TeacherId, TeacherId, Now);

        submission.UpdateAnswer("Fixed after feedback.", Deadline, Deadline.AddDays(1));

        submission.Answer.Should().Be("Fixed after feedback.");
        submission.Status.Should().Be(SubmissionStatus.Submitted);
    }

    [Fact]
    public void UpdateAnswer_WhenGraded_Throws()
    {
        var submission = CreateSubmission();
        submission.Grade(TeacherId, TeacherId, 100, 80, "Good work.", Now);

        var act = () => submission.UpdateAnswer("Edited answer.", Deadline, Now);

        act.Should().Throw<InvalidStateTransition>();
    }

    [Fact]
    public void ReturnForRevision_ByAssignmentTeacher_ReturnsAndClearsMarks()
    {
        var submission = CreateSubmission();
        submission.Grade(TeacherId, TeacherId, 100, 80, "Good work.", Now);

        submission.ReturnForRevision(TeacherId, TeacherId, Now);

        submission.Status.Should().Be(SubmissionStatus.Returned);
        submission.Marks.Should().BeNull();
        submission.Feedback.Should().BeNull();
        submission.GradedAt.Should().BeNull();
    }

    [Fact]
    public void ReturnForRevision_ByAnotherTeacher_Throws()
    {
        var submission = CreateSubmission();

        var act = () => submission.ReturnForRevision(Guid.NewGuid(), TeacherId, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*assignment's teacher*");
    }
}
