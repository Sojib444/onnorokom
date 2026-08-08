using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Mapping;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.Exceptions;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

public sealed class CreateSubmissionCommandHandler
    : IRequestHandler<CreateSubmissionCommand, SubmissionResponse>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private readonly ISubmissionWriteRepository _submissions;
    private readonly ISubmissionReadRepository _submissionLookups;
    private readonly IAssignmentReadRepository _assignments;
    private readonly IUserReadRepository _users;
    private readonly IFileStorage _fileStorage;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateSubmissionCommandHandler(
        ISubmissionWriteRepository submissions,
        ISubmissionReadRepository submissionLookups,
        IAssignmentReadRepository assignments,
        IUserReadRepository users,
        IFileStorage fileStorage,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _submissions = submissions;
        _submissionLookups = submissionLookups;
        _assignments = assignments;
        _users = users;
        _fileStorage = fileStorage;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<SubmissionResponse> Handle(
        CreateSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId!.Value;
        var classId = _currentUser.ClassId!.Value;

        var assignment = await _assignments.GetByIdAsync(request.AssignmentId, cancellationToken)
            ?? throw NotFoundException.For<Assignment>(request.AssignmentId);

        if (assignment.ClassId != classId)
        {
            throw new BusinessRuleViolation("This assignment is not for your class.");
        }

        if (await _submissionLookups.ExistsForAssignmentAndStudentAsync(
                request.AssignmentId,
                studentId,
                cancellationToken))
        {
            throw new BusinessRuleViolation("You have already submitted an answer to this assignment.");
        }

        var submission = Submission.Create(
            request.AssignmentId,
            studentId,
            request.Answer,
            assignment.Status == AssignmentStatus.Published,
            assignment.Deadline,
            DateTimeOffset.UtcNow);

        await AttachFileAsync(submission, request, cancellationToken);

        _submissions.Add(submission);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var student = await _users.GetByIdAsync(studentId, cancellationToken);

        return _mapper.Map<SubmissionResponse>(submission, options =>
        {
            options.Items[MapperContext.AssignmentTitle] = assignment.Title;
            options.Items[MapperContext.StudentName] = student?.FullName;
        });
    }

    /// <summary>
    /// Persists the attachment and links it to the submission.
    /// </summary>
    /// <remarks>
    /// The 10 MB cap is the server-side backstop for the form/file upload limit; it is
    /// enforced here so the limit holds even when the API is not behind the nginx proxy.
    /// File bytes are written to storage before the record is linked, so a failed write
    /// never leaves a dangling attachment row.
    /// </remarks>
    private async Task AttachFileAsync(
        Submission submission,
        CreateSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FileContent is null
            || string.IsNullOrWhiteSpace(request.FileName)
            || string.IsNullOrWhiteSpace(request.ContentType))
        {
            return;
        }

        if (request.FileContent.Length > MaxFileSizeBytes)
        {
            throw new BusinessRuleViolation(
                $"Attachments cannot exceed {MaxFileSizeBytes / 1024 / 1024} MB.");
        }

        var stored = await _fileStorage.SaveAsync(
            "submissions",
            Path.GetFileName(request.FileName),
            request.FileContent,
            request.ContentType,
            cancellationToken);

        submission.AddAttachment(new SubmissionAttachment(
            submission.Id,
            Path.GetFileName(request.FileName),
            stored.Path,
            request.ContentType,
            stored.Size));
    }
}
