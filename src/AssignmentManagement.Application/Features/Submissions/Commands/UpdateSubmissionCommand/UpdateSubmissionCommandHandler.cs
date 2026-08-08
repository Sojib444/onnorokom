using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Mapping;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Exceptions;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

public sealed class UpdateSubmissionCommandHandler
    : IRequestHandler<UpdateSubmissionCommand, SubmissionResponse>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private readonly ISubmissionWriteRepository _submissions;
    private readonly IAssignmentReadRepository _assignments;
    private readonly IUserReadRepository _users;
    private readonly IFileStorage _fileStorage;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateSubmissionCommandHandler(
        ISubmissionWriteRepository submissions,
        IAssignmentReadRepository assignments,
        IUserReadRepository users,
        IFileStorage fileStorage,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _submissions = submissions;
        _assignments = assignments;
        _users = users;
        _fileStorage = fileStorage;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<SubmissionResponse> Handle(
        UpdateSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await _submissions.GetByIdWithAttachmentsAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Submission>(request.Id);

        if (submission.StudentId != _currentUser.UserId)
        {
            throw new ForbiddenException("You can only edit your own submissions.");
        }

        var assignment = await _assignments.GetByIdAsync(submission.AssignmentId, cancellationToken)
            ?? throw NotFoundException.For<Assignment>(submission.AssignmentId);

        submission.UpdateAnswer(request.Answer, assignment.Deadline, DateTimeOffset.UtcNow);

        // A new file replaces the previous attachment entirely: the old rows are cleared
        // from the database first, then the replaced file's bytes are deleted from
        // storage. Deleting bytes is delegated to IFileStorage (rather than done here) so
        // the same handler works unchanged against object storage in a larger deployment.
        if (request.FileContent is not null)
        {
            if (request.FileContent.Length > MaxFileSizeBytes)
            {
                throw new BusinessRuleViolation(
                    $"Attachments cannot exceed {MaxFileSizeBytes / 1024 / 1024} MB.");
            }

            var oldPaths = submission.Attachments.Select(a => a.StoragePath).ToList();

            submission.ClearAttachments();

            var stored = await _fileStorage.SaveAsync(
                "submissions",
                Path.GetFileName(request.FileName ?? "attachment"),
                request.FileContent,
                request.ContentType ?? "application/octet-stream",
                cancellationToken);

            submission.AddAttachment(new SubmissionAttachment(
                submission.Id,
                Path.GetFileName(request.FileName ?? "attachment"),
                stored.Path,
                request.ContentType ?? "application/octet-stream",
                stored.Size));

            foreach (var oldPath in oldPaths)
            {
                await _fileStorage.DeleteAsync(oldPath, cancellationToken);
            }
        }

        _submissions.Update(submission);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var student = await _users.GetByIdAsync(submission.StudentId, cancellationToken);

        return _mapper.Map<SubmissionResponse>(submission, options =>
        {
            options.Items[MapperContext.AssignmentTitle] = assignment.Title;
            options.Items[MapperContext.StudentName] = student?.FullName;
        });
    }
}
