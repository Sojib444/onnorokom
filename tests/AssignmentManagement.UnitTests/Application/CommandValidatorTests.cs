using AssignmentManagement.Application.Features.Assignments;
using AssignmentManagement.Application.Features.Auth;
using AssignmentManagement.Application.Features.Submissions;
using AssignmentManagement.Application.Features.Users;
using FluentAssertions;

namespace AssignmentManagement.UnitTests.Application;

public sealed class CommandValidatorTests
{
    // ---- CreateAssignmentCommand ---------------------------------------------

    [Fact]
    public void CreateAssignment_ValidCommand_Passes()
    {
        var validator = new CreateAssignmentCommandValidator();

        var result = validator.Validate(new CreateAssignmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Title", "Description",
            DateTimeOffset.UtcNow.AddDays(1), 100));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateAssignment_EmptyTitle_Fails()
    {
        var validator = new CreateAssignmentCommandValidator();

        var result = validator.Validate(new CreateAssignmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), "", "Description",
            DateTimeOffset.UtcNow.AddDays(1), 100));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void CreateAssignment_PastDeadline_Fails()
    {
        var validator = new CreateAssignmentCommandValidator();

        var result = validator.Validate(new CreateAssignmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Title", "Description",
            DateTimeOffset.UtcNow.AddMinutes(-1), 100));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Deadline");
    }

    [Fact]
    public void CreateAssignment_NonPositiveMarks_Fails()
    {
        var validator = new CreateAssignmentCommandValidator();

        var result = validator.Validate(new CreateAssignmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Title", "Description",
            DateTimeOffset.UtcNow.AddDays(1), 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaximumMarks");
    }

    // ---- CreateSubmissionCommand ---------------------------------------------

    [Fact]
    public void CreateSubmission_EmptyAnswer_Fails()
    {
        var validator = new CreateSubmissionCommandValidator();

        var result = validator.Validate(new CreateSubmissionCommand(
            Guid.NewGuid(), "", null, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Answer");
    }

    // ---- GradeSubmissionCommand ----------------------------------------------

    [Fact]
    public void Grade_ValidCommand_Passes()
    {
        var validator = new GradeSubmissionCommandValidator();

        var result = validator.Validate(new GradeSubmissionCommand(Guid.NewGuid(), 85, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Grade_NegativeMarks_Fails()
    {
        var validator = new GradeSubmissionCommandValidator();

        var result = validator.Validate(new GradeSubmissionCommand(Guid.NewGuid(), -0.5m, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Marks");
    }

    [Fact]
    public void Grade_OverlongFeedback_Fails()
    {
        var validator = new GradeSubmissionCommandValidator();

        var result = validator.Validate(new GradeSubmissionCommand(
            Guid.NewGuid(), 85, new string('x', 2001)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Feedback");
    }

    // ---- LoginCommand ---------------------------------------------------------

    [Fact]
    public void Login_InvalidEmail_Fails()
    {
        var validator = new LoginCommandValidator();

        var result = validator.Validate(new LoginCommand("not-an-email", "password1"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Login_EmptyPassword_Fails()
    {
        var validator = new LoginCommandValidator();

        var result = validator.Validate(new LoginCommand("a@b.c", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    // ---- CreateUserCommand ----------------------------------------------------

    [Fact]
    public void CreateUser_ShortPassword_Fails()
    {
        var validator = new CreateUserCommandValidator();

        var result = validator.Validate(new CreateUserCommand(
            "Someone", "a@b.c", "short", "Teacher", null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void CreateUser_InvalidRole_Fails()
    {
        var validator = new CreateUserCommandValidator();

        var result = validator.Validate(new CreateUserCommand(
            "Someone", "a@b.c", "password1", "Superhero", null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }

    [Fact]
    public void CreateUser_StudentWithoutClass_Fails()
    {
        var validator = new CreateUserCommandValidator();

        var result = validator.Validate(new CreateUserCommand(
            "Someone", "a@b.c", "password1", "Student", null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClassId");
    }
}
