using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

/// <summary>Returns a single submission if the caller may view it.</summary>
public sealed record GetSubmissionByIdQuery(Guid Id) : IRequest<SubmissionResponse>;
