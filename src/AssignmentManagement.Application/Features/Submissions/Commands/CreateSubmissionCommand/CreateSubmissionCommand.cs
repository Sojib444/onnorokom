using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

/// <summary>
/// Submits an answer to an assignment, optionally with a file attachment. The student
/// must belong to the assignment's class and the assignment must be open for submission.
/// Students only.
/// </summary>
public sealed record CreateSubmissionCommand(
    Guid AssignmentId,
    string Answer,
    string? FileName,
    string? ContentType,
    Stream? FileContent) : IRequest<SubmissionResponse>;
