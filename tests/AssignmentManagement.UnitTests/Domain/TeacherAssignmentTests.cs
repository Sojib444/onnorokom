using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Exceptions;
using FluentAssertions;

namespace AssignmentManagement.UnitTests.Domain;

public sealed class TeacherAssignmentTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidIds_SetsAllReferences()
    {
        var allocation = new TeacherAssignment(TeacherId, ClassId, SubjectId);

        allocation.TeacherId.Should().Be(TeacherId);
        allocation.ClassId.Should().Be(ClassId);
        allocation.SubjectId.Should().Be(SubjectId);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void Create_WithEmptyIdentifier_Throws(bool emptyTeacher, bool emptyClass, bool emptySubject)
    {
        var teacher = emptyTeacher ? Guid.Empty : TeacherId;
        var classId = emptyClass ? Guid.Empty : ClassId;
        var subject = emptySubject ? Guid.Empty : SubjectId;

        var act = () => new TeacherAssignment(teacher, classId, subject);

        act.Should().Throw<BusinessRuleViolation>().WithMessage("*required*");
    }
}
