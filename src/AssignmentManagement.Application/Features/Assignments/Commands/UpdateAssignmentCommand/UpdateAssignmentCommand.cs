using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Assignments;

/// <summary>Updates a draft assignment owned by the caller. Teachers only.</summary>
public sealed record UpdateAssignmentCommand(
    Guid Id,
    Guid ClassId,
    Guid SubjectId,
    string Title,
    string Description,
    DateTimeOffset Deadline,
    decimal MaximumMarks) : IRequest<AssignmentResponse>;
