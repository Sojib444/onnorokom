using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Assignments;

/// <summary>
/// Creates a draft assignment. The teacher must be allocated to the class and subject
/// pair. Teachers only.
/// </summary>
public sealed record CreateAssignmentCommand(
    Guid ClassId,
    Guid SubjectId,
    string Title,
    string Description,
    DateTimeOffset Deadline,
    decimal MaximumMarks) : IRequest<AssignmentResponse>;
