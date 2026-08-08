using AssignmentManagement.Domain.Exceptions;
using AssignmentManagement.Domain.ValueObjects;
using FluentAssertions;

namespace AssignmentManagement.UnitTests.Domain;

public sealed class ValueObjectTests
{
    [Fact]
    public void EmailAddress_TrimsAndNormalizesToLowercase()
    {
        var email = new EmailAddress("  Jane.Doe@Example.COM  ");

        email.Value.Should().Be("jane.doe@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("plainaddress")]
    [InlineData("missing@domain")]
    [InlineData("a@b")]
    public void EmailAddress_WithInvalidValue_Throws(string value)
    {
        var act = () => new EmailAddress(value);

        act.Should().Throw<BusinessRuleViolation>();
    }

    [Fact]
    public void EmailAddress_Equality_IgnoresCaseAndSurroundingWhitespace()
    {
        var a = new EmailAddress("jane@example.com");
        var b = new EmailAddress("JANE@Example.COM");

        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Marks_WithinBounds_StoresValue()
    {
        var marks = new Marks(75, 100);

        marks.Value.Should().Be(75);
        marks.Maximum.Should().Be(100);
    }

    [Fact]
    public void Marks_EqualToMaximum_IsAllowed()
    {
        var marks = new Marks(100, 100);

        marks.Value.Should().Be(100);
    }

    [Fact]
    public void Marks_Negative_Throws()
    {
        var act = () => new Marks(-0.5m, 100);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*negative*");
    }

    [Fact]
    public void Marks_AboveMaximum_Throws()
    {
        var act = () => new Marks(100.5m, 100);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*maximum*");
    }

    [Fact]
    public void Marks_Equality_ComparesValueAndMaximum()
    {
        var a = new Marks(75, 100);
        var b = new Marks(75, 100);
        var c = new Marks(40, 50);

        a.Equals(b).Should().BeTrue();
        a.Equals(c).Should().BeFalse();
    }
}
