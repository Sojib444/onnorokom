using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.Events;
using AssignmentManagement.Domain.Exceptions;
using FluentAssertions;

namespace AssignmentManagement.UnitTests.Domain;

public sealed class AssignmentTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    private static Assignment CreateValidAssignment() =>
        new(TeacherId, ClassId, SubjectId, "Algebra Homework", "Solve exercises 1 to 10.",
            Now.AddDays(7), 100, Now);

    [Fact]
    public void Create_WithValidData_CreatesDraft()
    {
        var assignment = CreateValidAssignment();

        assignment.Status.Should().Be(AssignmentStatus.Draft);
        assignment.TeacherId.Should().Be(TeacherId);
        assignment.ClassId.Should().Be(ClassId);
        assignment.SubjectId.Should().Be(SubjectId);
        assignment.Title.Should().Be("Algebra Homework");
        assignment.MaximumMarks.Should().Be(100);
        assignment.Deadline.Should().Be(Now.AddDays(7));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitle_Throws(string title)
    {
        var act = () => new Assignment(TeacherId, ClassId, SubjectId, title, "Description.",
            Now.AddDays(7), 100, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*title*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyDescription_Throws(string description)
    {
        var act = () => new Assignment(TeacherId, ClassId, SubjectId, "Title", description,
            Now.AddDays(7), 100, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*description*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithNonPositiveMaximumMarks_Throws(decimal maximumMarks)
    {
        var act = () => new Assignment(TeacherId, ClassId, SubjectId, "Title", "Description.",
            Now.AddDays(7), maximumMarks, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*greater than zero*");
    }

    [Fact]
    public void Create_WithPastDeadline_Throws()
    {
        var act = () => new Assignment(TeacherId, ClassId, SubjectId, "Title", "Description.",
            Now.AddDays(-1), 100, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*future*");
    }

    [Fact]
    public void Create_WithEmptyTeacherId_Throws()
    {
        var act = () => new Assignment(Guid.Empty, ClassId, SubjectId, "Title", "Description.",
            Now.AddDays(7), 100, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*required*");
    }

    [Fact]
    public void Create_WithEmptyClassId_Throws()
    {
        var act = () => new Assignment(TeacherId, Guid.Empty, SubjectId, "Title", "Description.",
            Now.AddDays(7), 100, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*required*");
    }

    [Fact]
    public void Create_WithTitleOver200Characters_Throws()
    {
        var act = () => new Assignment(TeacherId, ClassId, SubjectId, new string('a', 201),
            "Description.", Now.AddDays(7), 100, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*200*");
    }

    [Fact]
    public void Publish_WhenDraft_PublishesAndRaisesEvent()
    {
        var assignment = CreateValidAssignment();

        assignment.Publish(Now);

        assignment.Status.Should().Be(AssignmentStatus.Published);
        assignment.DomainEvents.OfType<AssignmentPublished>().Should().ContainSingle()
            .Which.AssignmentId.Should().Be(assignment.Id);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_Throws()
    {
        var assignment = CreateValidAssignment();
        assignment.Publish(Now);

        var act = () => assignment.Publish(Now);

        act.Should().Throw<InvalidStateTransition>();
    }

    [Fact]
    public void Publish_WithPastDeadline_Throws()
    {
        // Created ten days ago with a deadline that has since passed.
        var assignment = new Assignment(TeacherId, ClassId, SubjectId, "Title", "Description.",
            Now.AddDays(-3), 100, Now.AddDays(-10));

        var act = () => assignment.Publish(Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*deadline*");
    }

    [Fact]
    public void Update_WhenDraft_UpdatesContent()
    {
        var assignment = CreateValidAssignment();
        var newDeadline = Now.AddDays(14);

        assignment.Update("New Title", "New description.", newDeadline, 50, ClassId, SubjectId, Now);

        assignment.Title.Should().Be("New Title");
        assignment.Description.Should().Be("New description.");
        assignment.Deadline.Should().Be(newDeadline);
        assignment.MaximumMarks.Should().Be(50);
    }

    [Fact]
    public void Update_WhenPublished_Throws()
    {
        var assignment = CreateValidAssignment();
        assignment.Publish(Now);

        var act = () => assignment.Update("New Title", "New description.",
            Now.AddDays(14), 50, ClassId, SubjectId, Now);

        act.Should().Throw<InvalidStateTransition>();
    }

    [Fact]
    public void EnsureCanBeDeleted_WhenDraft_DoesNotThrow()
    {
        var assignment = CreateValidAssignment();

        var act = () => assignment.EnsureCanBeDeleted();

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanBeDeleted_WhenPublished_Throws()
    {
        var assignment = CreateValidAssignment();
        assignment.Publish(Now);

        var act = () => assignment.EnsureCanBeDeleted();

        act.Should().Throw<InvalidStateTransition>();
    }

    [Fact]
    public void IsOpenForSubmission_WhenPublishedAndBeforeDeadline_IsTrue()
    {
        var assignment = CreateValidAssignment();
        assignment.Publish(Now);

        assignment.IsOpenForSubmission(Now.AddDays(3)).Should().BeTrue();
    }

    [Fact]
    public void IsOpenForSubmission_WhenDeadlinePassed_IsFalse()
    {
        var assignment = CreateValidAssignment();
        assignment.Publish(Now);

        assignment.IsOpenForSubmission(Now.AddDays(8)).Should().BeFalse();
    }

    [Fact]
    public void IsOpenForSubmission_WhenDraft_IsFalse()
    {
        var assignment = CreateValidAssignment();

        assignment.IsOpenForSubmission(Now).Should().BeFalse();
    }
}
