using MediatR;

namespace AssignmentManagement.Application.Features.Assignments;

/// <summary>Publishes a draft assignment owned by the caller. Teachers only.</summary>
public sealed record PublishAssignmentCommand(Guid Id) : IRequest<Unit>;
