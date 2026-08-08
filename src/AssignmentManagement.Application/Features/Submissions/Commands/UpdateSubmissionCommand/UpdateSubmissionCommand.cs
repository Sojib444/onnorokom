using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

/// <summary>
/// Updates the student's own answer before the deadline. A newly attached file replaces
/// any existing attachments. Students only.
/// </summary>
public sealed record UpdateSubmissionCommand(
    Guid Id,
    string Answer,
    string? FileName,
    string? ContentType,
    Stream? FileContent) : IRequest<SubmissionResponse>;
