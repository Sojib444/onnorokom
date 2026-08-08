using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.Events;
using AssignmentManagement.Domain.Exceptions;
using FluentAssertions;

namespace AssignmentManagement.UnitTests.Domain;

public sealed class GradingTests
{
    private const decimal MaximumMarks = 100;
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    private static Submission CreateSubmittedSubmission() =>
        Submission.Create(Guid.NewGuid(), Guid.NewGuid(), "My answer.",
            assignmentPublished: true, Now.AddDays(7), Now);

    [Fact]
    public void Grade_ByAssignmentTeacher_AwardsMarksAndFeedback()
    {
        var submission = CreateSubmittedSubmission();

        submission.Grade(TeacherId, TeacherId, MaximumMarks, 85.5m, "Well structured.", Now);

        submission.Status.Should().Be(SubmissionStatus.Graded);
        submission.Marks.Should().Be(85.5m);
        submission.Feedback.Should().Be("Well structured.");
        submission.GradedAt.Should().Be(Now);
        submission.DomainEvents.OfType<SubmissionGraded>().Should().ContainSingle().Which.Should()
            .Match<SubmissionGraded>(graded =>
                graded.SubmissionId == submission.Id && graded.Marks == 85.5m);
    }

    [Fact]
    public void Grade_WithMaximumMarks_AwardsExactlyTheCeiling()
    {
        var submission = CreateSubmittedSubmission();

        submission.Grade(TeacherId, TeacherId, MaximumMarks, MaximumMarks, null, Now);

        submission.Marks.Should().Be(MaximumMarks);
    }

    [Fact]
    public void Grade_WhenMarksExceedMaximum_Throws()
    {
        var submission = CreateSubmittedSubmission();

        var act = () => submission.Grade(TeacherId, TeacherId, MaximumMarks, 100.1m, null, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*maximum*");
    }

    [Fact]
    public void Grade_WithNegativeMarks_Throws()
    {
        var submission = CreateSubmittedSubmission();

        var act = () => submission.Grade(TeacherId, TeacherId, MaximumMarks, -1m, null, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*negative*");
    }

    [Fact]
    public void Grade_ByUnauthorizedTeacher_Throws()
    {
        var submission = CreateSubmittedSubmission();
        var otherTeacher = Guid.NewGuid();

        var act = () => submission.Grade(otherTeacher, TeacherId, MaximumMarks, 90, null, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*assignment's teacher*");
        submission.Marks.Should().BeNull();
    }

    [Fact]
    public void Grade_OnReturnedSubmission_GradesTheRevision()
    {
        var submission = CreateSubmittedSubmission();
        submission.ReturnForRevision(TeacherId, TeacherId, Now);

        submission.Grade(TeacherId, TeacherId, MaximumMarks, 95, "Great revision.", Now);

        submission.Status.Should().Be(SubmissionStatus.Graded);
        submission.Marks.Should().Be(95);
    }

    [Fact]
    public void Grade_Again_RegradesAndRaisesEvent()
    {
        var submission = CreateSubmittedSubmission();
        submission.Grade(TeacherId, TeacherId, MaximumMarks, 80, "First attempt.", Now);

        submission.Grade(TeacherId, TeacherId, MaximumMarks, 90, "After revision.", Now.AddHours(1));

        submission.Marks.Should().Be(90);
        submission.Feedback.Should().Be("After revision.");
        submission.DomainEvents.Should().Contain(e => e is SubmissionGraded);
    }
}
