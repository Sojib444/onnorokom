using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

/// <summary>Returns all submissions for an assignment. The assignment's teacher or an admin.</summary>
public sealed record GetAssignmentSubmissionsQuery(Guid AssignmentId)
    : IRequest<IReadOnlyList<SubmissionResponse>>;
