using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

/// <summary>Opens a submission attachment for download.</summary>
public sealed record DownloadAttachmentQuery(Guid SubmissionId, Guid AttachmentId)
    : IRequest<AttachmentDownloadResponse>;
