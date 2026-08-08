using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.Exceptions;
using AssignmentManagement.Domain.ValueObjects;
using FluentAssertions;

namespace AssignmentManagement.UnitTests.Domain;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid ClassId = Guid.NewGuid();

    private static EmailAddress Email(string value = "student@example.com") => new(value);

    [Fact]
    public void Create_StudentWithClass_SetsClassAndRole()
    {
        var user = new User("Jane Student", Email(), UserRole.Student, ClassId, Now);

        user.FullName.Should().Be("Jane Student");
        user.Role.Should().Be(UserRole.Student);
        user.ClassId.Should().Be(ClassId);
        user.CreatedAt.Should().Be(Now);
        user.PasswordHash.Should().BeEmpty();
    }

    [Fact]
    public void Create_Teacher_NeverCarriesClassAffiliation()
    {
        var user = new User("Mr. Teacher", Email(), UserRole.Teacher, ClassId, Now);

        user.ClassId.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyName_Throws()
    {
        var act = () => new User("  ", Email(), UserRole.Student, ClassId, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*name*");
    }

    [Fact]
    public void Create_WithInvalidEmail_Throws()
    {
        var act = () => new User("Jane", new EmailAddress("not-an-email"), UserRole.Student, ClassId, Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*email*");
    }

    [Fact]
    public void SetPasswordHash_StoresProvidedHash()
    {
        var user = new User("Jane", Email(), UserRole.Student, ClassId, Now);

        user.SetPasswordHash("$2a$11$hashedvalue", Now.AddMinutes(1));

        user.PasswordHash.Should().Be("$2a$11$hashedvalue");
        user.UpdatedAt.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void SetPasswordHash_WithEmptyValue_Throws()
    {
        var user = new User("Jane", Email(), UserRole.Student, ClassId, Now);

        var act = () => user.SetPasswordHash("  ", Now);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*hash*");
    }

    [Fact]
    public void UpdateProfile_ForStudent_ChangesClass()
    {
        var user = new User("Jane", Email(), UserRole.Student, ClassId, Now);
        var newClass = Guid.NewGuid();

        user.UpdateProfile("Jane Doe", newClass, Now);

        user.FullName.Should().Be("Jane Doe");
        user.ClassId.Should().Be(newClass);
    }

    [Fact]
    public void UpdateProfile_ForTeacher_IgnoresClass()
    {
        var user = new User("Mr. Teacher", Email(), UserRole.Teacher, ClassId, Now);

        user.UpdateProfile("Mr. Teacher", Guid.NewGuid(), Now);

        user.ClassId.Should().BeNull();
    }
}
