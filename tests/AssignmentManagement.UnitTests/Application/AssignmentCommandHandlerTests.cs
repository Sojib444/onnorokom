using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Features.Assignments;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.Exceptions;
using AutoMapper;
using FluentAssertions;
using Moq;
using static AssignmentManagement.UnitTests.Application.TestData;

namespace AssignmentManagement.UnitTests.Application;

public sealed class AssignmentCommandHandlerTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OtherTeacherId = Guid.NewGuid();
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();

    private readonly Mock<IAssignmentWriteRepository> _assignments = new();
    private readonly Mock<IAssignmentReadRepository> _assignmentsRead = new();
    private readonly Mock<ITeacherAssignmentReadRepository> _allocations = new();
    private readonly Mock<IClassReadRepository> _classes = new();
    private readonly Mock<ISubjectReadRepository> _subjects = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly IMapper _mapper = CreateMapper();

    private void GivenTeacher(Guid id = default)
    {
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Teacher);
        _currentUser.SetupGet(u => u.UserId).Returns(id == default ? TeacherId : id);
    }

    private CreateAssignmentCommandHandler CreateHandler() =>
        new(_assignments.Object, _allocations.Object, _classes.Object, _subjects.Object,
            _currentUser.Object, _unitOfWork.Object, _mapper);

    private UpdateAssignmentCommandHandler UpdateHandler() =>
        new(_assignments.Object, _allocations.Object, _classes.Object, _subjects.Object,
            _currentUser.Object, _unitOfWork.Object, _mapper);

    // ---- Create ---------------------------------------------------------------

    [Fact]
    public async Task Create_ByAllocatedTeacher_PersistsAndReturnsDraft()
    {
        GivenTeacher();
        _classes.Setup(r => r.GetByIdAsync(ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AClass());
        _subjects.Setup(r => r.GetByIdAsync(SubjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ASubject());
        _allocations.Setup(r => r.ExistsForTeacherAsync(
                TeacherId, ClassId, SubjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateAssignmentCommand(
            ClassId, SubjectId, "Algebra Test", "Solve chapter 8.",
            DateTimeOffset.UtcNow.AddDays(7), 100);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Status.Should().Be(nameof(AssignmentStatus.Draft));
        _assignments.Verify(r => r.Add(It.IsAny<Assignment>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ByUnallocatedTeacher_ThrowsBusinessRuleViolation()
    {
        GivenTeacher();
        _classes.Setup(r => r.GetByIdAsync(ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AClass());
        _subjects.Setup(r => r.GetByIdAsync(SubjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ASubject());
        _allocations.Setup(r => r.ExistsForTeacherAsync(
                TeacherId, ClassId, SubjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateAssignmentCommand(
            ClassId, SubjectId, "Algebra Test", "Solve chapter 8.",
            Now.AddDays(7), 100);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolation>().WithMessage("*not allocated*");
        _assignments.Verify(r => r.Add(It.IsAny<Assignment>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithMissingClass_ThrowsNotFound()
    {
        GivenTeacher();
        _classes.Setup(r => r.GetByIdAsync(ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Class?)null);

        var command = new CreateAssignmentCommand(
            ClassId, SubjectId, "Algebra Test", "Solve chapter 8.",
            Now.AddDays(7), 100);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_WithMissingSubject_ThrowsNotFound()
    {
        GivenTeacher();
        _classes.Setup(r => r.GetByIdAsync(ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AClass());
        _subjects.Setup(r => r.GetByIdAsync(SubjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subject?)null);

        var command = new CreateAssignmentCommand(
            ClassId, SubjectId, "Algebra Test", "Solve chapter 8.",
            Now.AddDays(7), 100);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---- Update ---------------------------------------------------------------

    [Fact]
    public async Task Update_ByOwner_UpdatesEntity()
    {
        GivenTeacher();
        var assignment = AnAssignment(TeacherId, ClassId, SubjectId);
        _assignments.Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);
        _classes.Setup(r => r.GetByIdAsync(ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AClass());
        _subjects.Setup(r => r.GetByIdAsync(SubjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ASubject());
        _allocations.Setup(r => r.ExistsForTeacherAsync(
                TeacherId, ClassId, SubjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new UpdateAssignmentCommand(
            assignment.Id, ClassId, SubjectId, "Revised Title", "New description.",
            Now.AddDays(21), 50);

        var result = await UpdateHandler().Handle(command, CancellationToken.None);

        result.Title.Should().Be("Revised Title");
        result.MaximumMarks.Should().Be(50);
    }

    [Fact]
    public async Task Update_ByNonOwner_ThrowsForbidden()
    {
        GivenTeacher(OtherTeacherId);
        var assignment = AnAssignment(TeacherId, ClassId, SubjectId);
        _assignments.Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var command = new UpdateAssignmentCommand(
            assignment.Id, ClassId, SubjectId, "Revised Title", "New description.",
            Now.AddDays(21), 50);

        var act = () => UpdateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _assignments.Verify(r => r.Update(It.IsAny<Assignment>()), Times.Never);
    }

    [Fact]
    public async Task Update_PublishedAssignment_ThrowsInvalidStateTransition()
    {
        GivenTeacher();
        var assignment = AnAssignment(TeacherId, ClassId, SubjectId);
        assignment.Publish(Now);
        _assignments.Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);
        _classes.Setup(r => r.GetByIdAsync(ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AClass());
        _subjects.Setup(r => r.GetByIdAsync(SubjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ASubject());
        _allocations.Setup(r => r.ExistsForTeacherAsync(
                TeacherId, ClassId, SubjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new UpdateAssignmentCommand(
            assignment.Id, ClassId, SubjectId, "Revised Title", "New description.",
            Now.AddDays(21), 50);

        var act = () => UpdateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidStateTransition>();
    }

    // ---- Publish --------------------------------------------------------------

    [Fact]
    public async Task Publish_ByOwner_PublishesAssignment()
    {
        GivenTeacher();
        var assignment = AnAssignment(TeacherId, ClassId, SubjectId);
        _assignments.Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var handler = new PublishAssignmentCommandHandler(
            _assignments.Object, _currentUser.Object, _unitOfWork.Object);

        await handler.Handle(new PublishAssignmentCommand(assignment.Id), CancellationToken.None);

        assignment.Status.Should().Be(AssignmentStatus.Published);
        _assignments.Verify(r => r.Update(assignment), Times.Once);
    }

    [Fact]
    public async Task Publish_ByNonOwner_ThrowsForbidden()
    {
        GivenTeacher(OtherTeacherId);
        var assignment = AnAssignment(TeacherId, ClassId, SubjectId);
        _assignments.Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var handler = new PublishAssignmentCommandHandler(
            _assignments.Object, _currentUser.Object, _unitOfWork.Object);

        var act = () => handler.Handle(new PublishAssignmentCommand(assignment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        assignment.Status.Should().Be(AssignmentStatus.Draft);
    }

    [Fact]
    public async Task Publish_PastDeadline_ThrowsBusinessRuleViolation()
    {
        GivenTeacher();
        var assignment = AnAssignment(
            TeacherId, ClassId, SubjectId,
            deadline: DateTimeOffset.UtcNow.AddMinutes(-5));
        _assignments.Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var handler = new PublishAssignmentCommandHandler(
            _assignments.Object, _currentUser.Object, _unitOfWork.Object);

        var act = () => handler.Handle(new PublishAssignmentCommand(assignment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolation>();
    }

    // ---- Delete ---------------------------------------------------------------

    [Fact]
    public async Task Delete_ByOwner_RemovesDraft()
    {
        GivenTeacher();
        var assignment = AnAssignment(TeacherId, ClassId, SubjectId);
        _assignments.Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var handler = new DeleteAssignmentCommandHandler(
            _assignments.Object, _currentUser.Object, _unitOfWork.Object);

        await handler.Handle(new DeleteAssignmentCommand(assignment.Id), CancellationToken.None);

        _assignments.Verify(r => r.Remove(assignment), Times.Once);
    }

    [Fact]
    public async Task Delete_ByNonOwner_ThrowsForbidden()
    {
        GivenTeacher(OtherTeacherId);
        var assignment = AnAssignment(TeacherId, ClassId, SubjectId);
        _assignments.Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var handler = new DeleteAssignmentCommandHandler(
            _assignments.Object, _currentUser.Object, _unitOfWork.Object);

        var act = () => handler.Handle(new DeleteAssignmentCommand(assignment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _assignments.Verify(r => r.Remove(It.IsAny<Assignment>()), Times.Never);
    }

    [Fact]
    public async Task Delete_PublishedAssignment_ThrowsInvalidStateTransition()
    {
        GivenTeacher();
        var assignment = AnAssignment(TeacherId, ClassId, SubjectId);
        assignment.Publish(Now);
        _assignments.Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var handler = new DeleteAssignmentCommandHandler(
            _assignments.Object, _currentUser.Object, _unitOfWork.Object);

        var act = () => handler.Handle(new DeleteAssignmentCommand(assignment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidStateTransition>();
    }

    // ---- Queries --------------------------------------------------------------

    [Fact]
    public async Task GetAssignments_AsAdmin_ReturnsAll()
    {
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Admin);
        _assignmentsRead.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { AnAssignment(TeacherId, ClassId, SubjectId) });

        var handler = new GetAssignmentsQueryHandler(_assignmentsRead.Object, _currentUser.Object, _mapper);

        var result = await handler.Handle(new GetAssignmentsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        _assignmentsRead.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAssignments_AsTeacher_ReturnsOwnOnly()
    {
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Teacher);
        _currentUser.SetupGet(u => u.UserId).Returns(TeacherId);
        _assignmentsRead.Setup(r => r.GetByTeacherAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { AnAssignment(TeacherId, ClassId, SubjectId) });

        var handler = new GetAssignmentsQueryHandler(_assignmentsRead.Object, _currentUser.Object, _mapper);

        var result = await handler.Handle(new GetAssignmentsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        _assignmentsRead.Verify(r => r.GetByTeacherAsync(TeacherId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAssignments_AsStudent_ReturnsPublishedForClass()
    {
        var studentId = Guid.NewGuid();
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Student);
        _currentUser.SetupGet(u => u.UserId).Returns(studentId);
        _currentUser.SetupGet(u => u.ClassId).Returns(ClassId);
        _assignmentsRead.Setup(r => r.GetPublishedForClassAsync(ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { AnAssignment(TeacherId, ClassId, SubjectId) });

        var handler = new GetAssignmentsQueryHandler(_assignmentsRead.Object, _currentUser.Object, _mapper);

        var result = await handler.Handle(new GetAssignmentsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        _assignmentsRead.Verify(
            r => r.GetPublishedForClassAsync(ClassId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetById_AsStudentFromAnotherClass_ThrowsForbidden()
    {
        var otherClass = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Student);
        _currentUser.SetupGet(u => u.UserId).Returns(studentId);
        _currentUser.SetupGet(u => u.ClassId).Returns(otherClass);

        var assignment = AnAssignment(TeacherId, ClassId, SubjectId);
        _assignmentsRead.Setup(r => r.GetByIdWithDetailsAsync(assignment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var handler = new GetAssignmentByIdQueryHandler(_assignmentsRead.Object, _currentUser.Object, _mapper);

        var act = () => handler.Handle(new GetAssignmentByIdQuery(assignment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
