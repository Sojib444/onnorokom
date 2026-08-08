using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

/// <summary>Returns a submission to the student for revision. The assignment's teacher only.</summary>
public sealed record ReturnSubmissionCommand(Guid Id) : IRequest<SubmissionResponse>;
