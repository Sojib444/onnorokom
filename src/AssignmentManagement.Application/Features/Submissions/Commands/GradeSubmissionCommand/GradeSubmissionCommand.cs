using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

/// <summary>Grades a submission. The assignment's teacher only.</summary>
public sealed record GradeSubmissionCommand(Guid Id, decimal Marks, string? Feedback)
    : IRequest<SubmissionResponse>;
