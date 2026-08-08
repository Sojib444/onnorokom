using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

/// <summary>Returns the caller's own submissions, newest first. Students only.</summary>
public sealed record GetMySubmissionsQuery : IRequest<IReadOnlyList<SubmissionResponse>>;
