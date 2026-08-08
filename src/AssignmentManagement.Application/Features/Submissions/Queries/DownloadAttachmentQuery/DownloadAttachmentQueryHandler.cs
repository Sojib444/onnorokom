using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

public sealed class DownloadAttachmentQueryHandler
    : IRequestHandler<DownloadAttachmentQuery, AttachmentDownloadResponse>
{
    private readonly ISubmissionReadRepository _submissions;
    private readonly IAssignmentReadRepository _assignments;
    private readonly IFileStorage _fileStorage;
    private readonly ICurrentUser _currentUser;

    public DownloadAttachmentQueryHandler(
        ISubmissionReadRepository submissions,
        IAssignmentReadRepository assignments,
        IFileStorage fileStorage,
        ICurrentUser currentUser)
    {
        _submissions = submissions;
        _assignments = assignments;
        _fileStorage = fileStorage;
        _currentUser = currentUser;
    }

    public async Task<AttachmentDownloadResponse> Handle(
        DownloadAttachmentQuery request,
        CancellationToken cancellationToken)
    {
        var submission = await _submissions.GetByIdWithAttachmentsAsync(request.SubmissionId, cancellationToken)
            ?? throw NotFoundException.For<Submission>(request.SubmissionId);

        var assignment = await _assignments.GetByIdAsync(submission.AssignmentId, cancellationToken);

        // Authorization mirrors GetSubmissionById: admins may download anything, teachers
        // only files for assignments they own, students only their own files. The requested
        // attachment is then resolved INSIDE the already-authorized submission, so a caller
        // can never fetch a file that belongs to another submission.
        switch (_currentUser.Role)
        {
            case UserRole.Admin:
                break;
            case UserRole.Teacher:
                if (assignment is null || assignment.TeacherId != _currentUser.UserId)
                {
                    throw new ForbiddenException("You can only download files from your own assignments.");
                }
                break;
            default:
                if (submission.StudentId != _currentUser.UserId)
                {
                    throw new ForbiddenException("You can only download your own files.");
                }
                break;
        }

        var attachment = submission.Attachments.FirstOrDefault(a => a.Id == request.AttachmentId)
            ?? throw NotFoundException.For<SubmissionAttachment>(request.AttachmentId);

        var stream = await _fileStorage.GetAsync(attachment.StoragePath, cancellationToken)
            ?? throw NotFoundException.For<SubmissionAttachment>(request.AttachmentId);

        return new AttachmentDownloadResponse(stream, attachment.FileName, attachment.ContentType);
    }
}
