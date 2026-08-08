using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Features.Users;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Exceptions;
using AutoMapper;
using FluentAssertions;
using Moq;
using static AssignmentManagement.UnitTests.Application.TestData;

namespace AssignmentManagement.UnitTests.Application;

public sealed class UserCommandHandlerTests
{
    private static readonly Guid ClassId = Guid.NewGuid();

    private readonly Mock<IUserWriteRepository> _users = new();
    private readonly Mock<IUserReadRepository> _userLookups = new();
    private readonly Mock<IClassReadRepository> _classes = new();
    private readonly Mock<IAssignmentReadRepository> _assignments = new();
    private readonly Mock<ISubmissionReadRepository> _submissions = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly IMapper _mapper = CreateMapper();

    // ---- Create ---------------------------------------------------------------

    [Fact]
    public async Task Create_WithNewEmail_CreatesUser()
    {
        _userLookups.Setup(r => r.ExistsByEmailAsync("new@school.edu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash("password123")).Returns("hashed");

        var handler = new CreateUserCommandHandler(
            _users.Object, _userLookups.Object, _classes.Object,
            _passwordHasher.Object, _unitOfWork.Object, _mapper);

        var command = new CreateUserCommand(
            "New Teacher", "new@school.edu", "password123", "Teacher", null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.FullName.Should().Be("New Teacher");
        result.Role.Should().Be("Teacher");
        _users.Verify(r => r.Add(It.Is<User>(u => u.PasswordHash == "hashed")), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ThrowsBusinessRuleViolation()
    {
        _userLookups.Setup(r => r.ExistsByEmailAsync("dup@school.edu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateUserCommandHandler(
            _users.Object, _userLookups.Object, _classes.Object,
            _passwordHasher.Object, _unitOfWork.Object, _mapper);

        var act = () => handler.Handle(
            new CreateUserCommand("Dup", "dup@school.edu", "password123", "Teacher", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolation>().WithMessage("*already exists*");
        _users.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Create_StudentWithMissingClass_ThrowsNotFound()
    {
        _userLookups.Setup(r => r.ExistsByEmailAsync("stu@school.edu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _classes.Setup(r => r.ExistsAsync(ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateUserCommandHandler(
            _users.Object, _userLookups.Object, _classes.Object,
            _passwordHasher.Object, _unitOfWork.Object, _mapper);

        var act = () => handler.Handle(
            new CreateUserCommand("Student", "stu@school.edu", "password123", "Student", ClassId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---- Update ---------------------------------------------------------------

    [Fact]
    public async Task Update_ExistingStudent_UpdatesProfile()
    {
        var user = AStudent("Nusrat Jahan", "stu@school.edu", classId: ClassId);
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _classes.Setup(r => r.ExistsAsync(ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new UpdateUserCommandHandler(
            _users.Object, _classes.Object, _unitOfWork.Object, _mapper);

        var result = await handler.Handle(
            new UpdateUserCommand(user.Id, "Nusrat J. Rahman", ClassId), CancellationToken.None);

        result.FullName.Should().Be("Nusrat J. Rahman");
    }

    [Fact]
    public async Task Update_MissingUser_ThrowsNotFound()
    {
        _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new UpdateUserCommandHandler(
            _users.Object, _classes.Object, _unitOfWork.Object, _mapper);

        var act = () => handler.Handle(
            new UpdateUserCommand(Guid.NewGuid(), "Someone", null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---- Password -------------------------------------------------------------

    [Fact]
    public async Task ResetPassword_UpdatesHash()
    {
        var user = ATeacher();
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Hash("newPassword1")).Returns("new-hash");

        var handler = new UpdateUserPasswordCommandHandler(
            _users.Object, _passwordHasher.Object, _unitOfWork.Object);

        await handler.Handle(
            new UpdateUserPasswordCommand(user.Id, "newPassword1"), CancellationToken.None);

        user.PasswordHash.Should().Be("new-hash");
        _users.Verify(r => r.Update(user), Times.Once);
    }

    // ---- Delete ---------------------------------------------------------------

    [Fact]
    public async Task Delete_TeacherWithoutAssignments_RemovesUser()
    {
        var user = ATeacher();
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _assignments.Setup(r => r.ExistsForTeacherAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new DeleteUserCommandHandler(
            _users.Object, _assignments.Object, _submissions.Object, _unitOfWork.Object);

        await handler.Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        _users.Verify(r => r.Remove(user), Times.Once);
    }

    [Fact]
    public async Task Delete_TeacherWithAssignments_ThrowsBusinessRuleViolation()
    {
        var user = ATeacher();
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _assignments.Setup(r => r.ExistsForTeacherAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new DeleteUserCommandHandler(
            _users.Object, _assignments.Object, _submissions.Object, _unitOfWork.Object);

        var act = () => handler.Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolation>().WithMessage("*cannot be deleted*");
        _users.Verify(r => r.Remove(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Delete_StudentWithSubmissions_ThrowsBusinessRuleViolation()
    {
        var user = AStudent(classId: ClassId);
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _submissions.Setup(r => r.ExistsForStudentAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new DeleteUserCommandHandler(
            _users.Object, _assignments.Object, _submissions.Object, _unitOfWork.Object);

        var act = () => handler.Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolation>().WithMessage("*cannot be deleted*");
    }
}
