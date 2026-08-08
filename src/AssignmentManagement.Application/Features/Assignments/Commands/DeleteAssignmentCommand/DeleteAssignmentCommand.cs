using MediatR;

namespace AssignmentManagement.Application.Features.Assignments;

/// <summary>Deletes a draft assignment owned by the caller. Teachers only.</summary>
public sealed record DeleteAssignmentCommand(Guid Id) : IRequest<Unit>;
