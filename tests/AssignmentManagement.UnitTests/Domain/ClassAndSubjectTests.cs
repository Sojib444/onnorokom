using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Exceptions;
using FluentAssertions;

namespace AssignmentManagement.UnitTests.Domain;

public sealed class ClassAndSubjectTests
{
    [Fact]
    public void Class_Create_WithValidData_SetsNameAndDescription()
    {
        var klass = new Class("Grade 7", "Morning section");

        klass.Name.Should().Be("Grade 7");
        klass.Description.Should().Be("Morning section");
    }

    [Fact]
    public void Class_Create_WithEmptyDescription_StoresNull()
    {
        var klass = new Class("Grade 7", "   ");

        klass.Description.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Class_Create_WithEmptyName_Throws(string name)
    {
        var act = () => new Class(name, "Description");

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*class name*");
    }

    [Fact]
    public void Class_Update_ChangesFields()
    {
        var klass = new Class("Grade 7", "Old description");

        klass.Update("Grade 8", "New description");

        klass.Name.Should().Be("Grade 8");
        klass.Description.Should().Be("New description");
    }

    [Fact]
    public void Subject_Create_WithValidData_SetsNameAndCode()
    {
        var subject = new Subject("Mathematics", "MATH-101");

        subject.Name.Should().Be("Mathematics");
        subject.Code.Should().Be("MATH-101");
    }

    [Theory]
    [InlineData("", "MATH-101")]
    [InlineData("Mathematics", "")]
    [InlineData("  ", "   ")]
    public void Subject_Create_WithEmptyValue_Throws(string name, string code)
    {
        var act = () => new Subject(name, code);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*required*");
    }
}
