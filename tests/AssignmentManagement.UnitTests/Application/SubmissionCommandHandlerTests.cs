using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Features.Submissions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.Exceptions;
using AutoMapper;
using FluentAssertions;
using Moq;
using static AssignmentManagement.UnitTests.Application.TestData;

namespace AssignmentManagement.UnitTests.Application;

public sealed class SubmissionCommandHandlerTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OtherTeacherId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid OtherStudentId = Guid.NewGuid();
    private static readonly Guid ClassId = Guid.NewGuid();

    private static readonly DateTimeOffset Deadline = DateTimeOffset.UtcNow.AddDays(7);

    private readonly Mock<ISubmissionWriteRepository> _submissions = new();
    private readonly Mock<ISubmissionReadRepository> _submissionReads = new();
    private readonly Mock<IAssignmentReadRepository> _assignments = new();
    private readonly Mock<IUserReadRepository> _users = new();
    private readonly Mock<IFileStorage> _fileStorage = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly IMapper _mapper = CreateMapper();

    private void GivenTeacher(Guid id = default)
    {
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Teacher);
        _currentUser.SetupGet(u => u.UserId).Returns(id == default ? TeacherId : id);
    }

    private void GivenStudent(Guid id = default)
    {
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Student);
        _currentUser.SetupGet(u => u.UserId).Returns(id == default ? StudentId : id);
        _currentUser.SetupGet(u => u.ClassId).Returns(ClassId);
    }

    private void GivenAdmin()
    {
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Admin);
        _currentUser.SetupGet(u => u.UserId).Returns(Guid.NewGuid());
    }

    private void StubGetAssignment(Assignment assignment) =>
        _assignments.Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

    private void StubGetSubmission(Submission submission)
    {
        _submissions.Setup(r => r.GetByIdWithAttachmentsAsync(
                submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionReads.Setup(r => r.GetByIdWithAttachmentsAsync(
                submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
    }

    private CreateSubmissionCommandHandler CreateHandler() =>
        new(_submissions.Object, _submissionReads.Object, _assignments.Object, _users.Object,
            _fileStorage.Object, _currentUser.Object, _unitOfWork.Object, _mapper);

    private UpdateSubmissionCommandHandler UpdateHandler() =>
        new(_submissions.Object, _assignments.Object, _users.Object,
            _fileStorage.Object, _currentUser.Object, _unitOfWork.Object, _mapper);

    // ---- Create ---------------------------------------------------------------

    [Fact]
    public async Task Create_ForPublishedAssignment_PersistsSubmission()
    {
        GivenStudent();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid());
        assignment.Publish(Now);
        StubGetAssignment(assignment);
        _submissionReads.Setup(r => r.ExistsForAssignmentAndStudentAsync(
                assignment.Id, StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateSubmissionCommand(assignment.Id, "x = 4", null, null, null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Status.Should().Be(nameof(SubmissionStatus.Submitted));
        _submissions.Verify(r => r.Add(It.IsAny<Submission>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ByStudentFromAnotherClass_ThrowsBusinessRuleViolation()
    {
        GivenStudent();
        var assignment = AnAssignment(TeacherId, Guid.NewGuid(), Guid.NewGuid());
        assignment.Publish(Now);
        StubGetAssignment(assignment);

        var command = new CreateSubmissionCommand(assignment.Id, "x = 4", null, null, null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolation>().WithMessage("*not for your class*");
        _submissions.Verify(r => r.Add(It.IsAny<Submission>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenAlreadySubmitted_ThrowsBusinessRuleViolation()
    {
        GivenStudent();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid());
        assignment.Publish(Now);
        StubGetAssignment(assignment);
        _submissionReads.Setup(r => r.ExistsForAssignmentAndStudentAsync(
                assignment.Id, StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateSubmissionCommand(assignment.Id, "x = 4", null, null, null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolation>().WithMessage("*already submitted*");
    }

    [Fact]
    public async Task Create_ForDraftAssignment_ThrowsBusinessRuleViolation()
    {
        GivenStudent();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid());
        StubGetAssignment(assignment);

        var command = new CreateSubmissionCommand(assignment.Id, "x = 4", null, null, null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolation>().WithMessage("*must be published*");
    }

    [Fact]
    public async Task Create_AfterDeadline_ThrowsBusinessRuleViolation()
    {
        GivenStudent();
        var assignment = AnAssignment(
            TeacherId, ClassId, Guid.NewGuid(),
            deadline: DateTimeOffset.UtcNow.AddMinutes(-5));
        assignment.Publish(TestData.Now);
        StubGetAssignment(assignment);

        var command = new CreateSubmissionCommand(assignment.Id, "x = 4", null, null, null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolation>().WithMessage("*deadline has passed*");
    }

    [Fact]
    public async Task Create_WithAttachment_SavesFileAndAddsAttachment()
    {
        GivenStudent();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid());
        assignment.Publish(Now);
        StubGetAssignment(assignment);
        _submissionReads.Setup(r => r.ExistsForAssignmentAndStudentAsync(
                assignment.Id, StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var content = new MemoryStream([1, 2, 3]);
        _fileStorage.Setup(f => f.SaveAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFile("submissions/sol.pdf", 3));

        var command = new CreateSubmissionCommand(
            assignment.Id, "x = 4", "solution.pdf", "application/pdf", content);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Attachments.Should().ContainSingle().Which.FileName.Should().Be("solution.pdf");
        _fileStorage.Verify(f => f.SaveAsync(
            "submissions", "solution.pdf", content, "application/pdf",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithOversizedAttachment_ThrowsBusinessRuleViolation()
    {
        GivenStudent();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid());
        assignment.Publish(Now);
        StubGetAssignment(assignment);
        _submissionReads.Setup(r => r.ExistsForAssignmentAndStudentAsync(
                assignment.Id, StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var oversized = new MemoryStream(new byte[10 * 1024 * 1024 + 1]);

        var command = new CreateSubmissionCommand(
            assignment.Id, "x = 4", "solution.pdf", "application/pdf", oversized);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolation>().WithMessage("*cannot exceed*");
        _fileStorage.Verify(f => f.SaveAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithMissingAssignment_ThrowsNotFound()
    {
        GivenStudent();
        _assignments.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Assignment?)null);

        var command = new CreateSubmissionCommand(Guid.NewGuid(), "x = 4", null, null, null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---- Update ---------------------------------------------------------------

    [Fact]
    public async Task Update_OwnSubmissionBeforeDeadline_UpdatesAnswer()
    {
        GivenStudent();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        StubGetSubmission(submission);

        var command = new UpdateSubmissionCommand(submission.Id, "x = 5", null, null, null);

        var result = await UpdateHandler().Handle(command, CancellationToken.None);

        result.Answer.Should().Be("x = 5");
        _submissions.Verify(r => r.Update(submission), Times.Once);
    }

    [Fact]
    public async Task Update_SomeoneElsesSubmission_ThrowsForbidden()
    {
        GivenStudent();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, OtherStudentId, Deadline);
        StubGetSubmission(submission);

        var command = new UpdateSubmissionCommand(submission.Id, "x = 5", null, null, null);

        var act = () => UpdateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _submissions.Verify(r => r.Update(It.IsAny<Submission>()), Times.Never);
    }

    [Fact]
    public async Task Update_AfterDeadline_ThrowsBusinessRuleViolation()
    {
        GivenStudent();
        var pastDeadline = DateTimeOffset.UtcNow.AddMinutes(-5);
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), pastDeadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, pastDeadline);
        StubGetSubmission(submission);

        var command = new UpdateSubmissionCommand(submission.Id, "x = 5", null, null, null);

        var act = () => UpdateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolation>().WithMessage("*deadline has passed*");
    }

    [Fact]
    public async Task Update_GradedSubmission_ThrowsInvalidStateTransition()
    {
        GivenStudent();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        submission.Grade(TeacherId, TeacherId, 100, 80, "Good.", Now);
        StubGetSubmission(submission);

        var command = new UpdateSubmissionCommand(submission.Id, "x = 5", null, null, null);

        var act = () => UpdateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidStateTransition>();
    }

    [Fact]
    public async Task Update_ReturnedSubmission_PastDeadline_Resubmits()
    {
        GivenStudent();
        var pastDeadline = DateTimeOffset.UtcNow.AddMinutes(-5);
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), pastDeadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, pastDeadline);
        submission.ReturnForRevision(TeacherId, TeacherId, Now);
        StubGetSubmission(submission);

        var command = new UpdateSubmissionCommand(submission.Id, "Revised answer.", null, null, null);

        var result = await UpdateHandler().Handle(command, CancellationToken.None);

        result.Answer.Should().Be("Revised answer.");
        result.Status.Should().Be(nameof(SubmissionStatus.Submitted));
    }

    [Fact]
    public async Task Update_WithReplacementFile_DeletesOldFile()
    {
        GivenStudent();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        submission.AddAttachment(new SubmissionAttachment(
            submission.Id, "old.pdf", "submissions/old.pdf", "application/pdf", 4));
        StubGetSubmission(submission);
        _fileStorage.Setup(f => f.SaveAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFile("submissions/new.pdf", 5));

        var content = new MemoryStream([9, 9, 9, 9, 9]);
        var command = new UpdateSubmissionCommand(
            submission.Id, "x = 5", "new.pdf", "application/pdf", content);

        await UpdateHandler().Handle(command, CancellationToken.None);

        _fileStorage.Verify(f => f.DeleteAsync("submissions/old.pdf", It.IsAny<CancellationToken>()), Times.Once);
        submission.Attachments.Should().ContainSingle().Which.FileName.Should().Be("new.pdf");
    }

    // ---- Grade ----------------------------------------------------------------

    [Fact]
    public async Task Grade_ByAssignmentTeacher_AwardsMarks()
    {
        GivenTeacher();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline, maximumMarks: 100);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        StubGetSubmission(submission);

        var handler = new GradeSubmissionCommandHandler(
            _submissions.Object, _assignments.Object, _users.Object,
            _currentUser.Object, _unitOfWork.Object, _mapper);

        var result = await handler.Handle(
            new GradeSubmissionCommand(submission.Id, 85.5m, "Well structured."), CancellationToken.None);

        result.Marks.Should().Be(85.5m);
        result.Status.Should().Be(nameof(SubmissionStatus.Graded));
    }

    [Fact]
    public async Task Grade_WhenMarksExceedMaximum_ThrowsBusinessRuleViolation()
    {
        GivenTeacher();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline, maximumMarks: 100);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        StubGetSubmission(submission);

        var handler = new GradeSubmissionCommandHandler(
            _submissions.Object, _assignments.Object, _users.Object,
            _currentUser.Object, _unitOfWork.Object, _mapper);

        var act = () => handler.Handle(
            new GradeSubmissionCommand(submission.Id, 100.1m, null), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolation>().WithMessage("*maximum*");
    }

    [Fact]
    public async Task Grade_WithNegativeMarks_ThrowsBusinessRuleViolation()
    {
        GivenTeacher();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline, maximumMarks: 100);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        StubGetSubmission(submission);

        var handler = new GradeSubmissionCommandHandler(
            _submissions.Object, _assignments.Object, _users.Object,
            _currentUser.Object, _unitOfWork.Object, _mapper);

        var act = () => handler.Handle(
            new GradeSubmissionCommand(submission.Id, -1m, null), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolation>().WithMessage("*negative*");
    }

    [Fact]
    public async Task Grade_ByTeacherOfAnotherAssignment_ThrowsForbidden()
    {
        GivenTeacher(OtherTeacherId);
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        StubGetSubmission(submission);

        var handler = new GradeSubmissionCommandHandler(
            _submissions.Object, _assignments.Object, _users.Object,
            _currentUser.Object, _unitOfWork.Object, _mapper);

        var act = () => handler.Handle(
            new GradeSubmissionCommand(submission.Id, 90, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        submission.Marks.Should().BeNull();
    }

    // ---- Return ---------------------------------------------------------------

    [Fact]
    public async Task Return_ByAssignmentTeacher_ReturnsForRevision()
    {
        GivenTeacher();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        submission.Grade(TeacherId, TeacherId, 100, 70, "Try again.", Now);
        StubGetSubmission(submission);

        var handler = new ReturnSubmissionCommandHandler(
            _submissions.Object, _assignments.Object, _users.Object,
            _currentUser.Object, _unitOfWork.Object, _mapper);

        var result = await handler.Handle(new ReturnSubmissionCommand(submission.Id), CancellationToken.None);

        result.Status.Should().Be(nameof(SubmissionStatus.Returned));
        result.Marks.Should().BeNull();
    }

    [Fact]
    public async Task Return_ByTeacherOfAnotherAssignment_ThrowsForbidden()
    {
        GivenTeacher(OtherTeacherId);
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        StubGetSubmission(submission);

        var handler = new ReturnSubmissionCommandHandler(
            _submissions.Object, _assignments.Object, _users.Object,
            _currentUser.Object, _unitOfWork.Object, _mapper);

        var act = () => handler.Handle(new ReturnSubmissionCommand(submission.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ---- Get by id ------------------------------------------------------------

    [Fact]
    public async Task GetById_ByOwnStudent_ReturnsSubmission()
    {
        GivenStudent();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        StubGetSubmission(submission);

        var handler = new GetSubmissionByIdQueryHandler(
            _submissionReads.Object, _assignments.Object, _users.Object, _currentUser.Object, _mapper);

        var result = await handler.Handle(new GetSubmissionByIdQuery(submission.Id), CancellationToken.None);

        result.Id.Should().Be(submission.Id);
    }

    [Fact]
    public async Task GetById_ByAnotherStudent_ThrowsForbidden()
    {
        GivenStudent();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, OtherStudentId, Deadline);
        StubGetSubmission(submission);

        var handler = new GetSubmissionByIdQueryHandler(
            _submissionReads.Object, _assignments.Object, _users.Object, _currentUser.Object, _mapper);

        var act = () => handler.Handle(new GetSubmissionByIdQuery(submission.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetById_ByAssignmentTeacher_ReturnsSubmission()
    {
        GivenTeacher();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        StubGetSubmission(submission);

        var handler = new GetSubmissionByIdQueryHandler(
            _submissionReads.Object, _assignments.Object, _users.Object, _currentUser.Object, _mapper);

        var result = await handler.Handle(new GetSubmissionByIdQuery(submission.Id), CancellationToken.None);

        result.Id.Should().Be(submission.Id);
    }

    [Fact]
    public async Task GetById_ByUnrelatedTeacher_ThrowsForbidden()
    {
        GivenTeacher(OtherTeacherId);
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        StubGetSubmission(submission);

        var handler = new GetSubmissionByIdQueryHandler(
            _submissionReads.Object, _assignments.Object, _users.Object, _currentUser.Object, _mapper);

        var act = () => handler.Handle(new GetSubmissionByIdQuery(submission.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetById_ByAdmin_ReturnsSubmission()
    {
        GivenAdmin();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        StubGetSubmission(submission);

        var handler = new GetSubmissionByIdQueryHandler(
            _submissionReads.Object, _assignments.Object, _users.Object, _currentUser.Object, _mapper);

        var result = await handler.Handle(new GetSubmissionByIdQuery(submission.Id), CancellationToken.None);

        result.Id.Should().Be(submission.Id);
    }

    [Fact]
    public async Task GetById_WhenMissing_ThrowsNotFound()
    {
        GivenStudent();
        _submissionReads.Setup(r => r.GetByIdWithAttachmentsAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission?)null);

        var handler = new GetSubmissionByIdQueryHandler(
            _submissionReads.Object, _assignments.Object, _users.Object, _currentUser.Object, _mapper);

        var act = () => handler.Handle(new GetSubmissionByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---- Download attachment --------------------------------------------------

    [Fact]
    public async Task Download_ByOwnStudent_ReturnsFile()
    {
        GivenStudent();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        var attachment = new SubmissionAttachment(
            submission.Id, "sol.pdf", "submissions/sol.pdf", "application/pdf", 3);
        submission.AddAttachment(attachment);
        StubGetSubmission(submission);
        _fileStorage.Setup(f => f.GetAsync(attachment.StoragePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([1, 2, 3]));

        var handler = new DownloadAttachmentQueryHandler(
            _submissionReads.Object, _assignments.Object, _fileStorage.Object, _currentUser.Object);

        var result = await handler.Handle(
            new DownloadAttachmentQuery(submission.Id, attachment.Id), CancellationToken.None);

        result.FileName.Should().Be("sol.pdf");
        result.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task Download_FromAnotherStudentsSubmission_ThrowsForbidden()
    {
        GivenStudent();
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, OtherStudentId, Deadline);
        var attachment = new SubmissionAttachment(
            submission.Id, "sol.pdf", "submissions/sol.pdf", "application/pdf", 3);
        submission.AddAttachment(attachment);
        StubGetSubmission(submission);

        var handler = new DownloadAttachmentQueryHandler(
            _submissionReads.Object, _assignments.Object, _fileStorage.Object, _currentUser.Object);

        var act = () => handler.Handle(
            new DownloadAttachmentQuery(submission.Id, attachment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _fileStorage.Verify(f => f.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Download_ByUnrelatedTeacher_ThrowsForbidden()
    {
        GivenTeacher(OtherTeacherId);
        var assignment = AnAssignment(TeacherId, ClassId, Guid.NewGuid(), Deadline);
        StubGetAssignment(assignment);
        var submission = ASubmission(assignment.Id, StudentId, Deadline);
        var attachment = new SubmissionAttachment(
            submission.Id, "sol.pdf", "submissions/sol.pdf", "application/pdf", 3);
        submission.AddAttachment(attachment);
        StubGetSubmission(submission);

        var handler = new DownloadAttachmentQueryHandler(
            _submissionReads.Object, _assignments.Object, _fileStorage.Object, _currentUser.Object);

        var act = () => handler.Handle(
            new DownloadAttachmentQuery(submission.Id, attachment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
