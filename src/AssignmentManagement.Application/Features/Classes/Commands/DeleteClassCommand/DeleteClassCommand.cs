using MediatR;

namespace AssignmentManagement.Application.Features.Classes;

/// <summary>
/// Deletes a class. A class that already has assignments or enrolled students cannot be
/// deleted because the historical records and memberships must remain intact.
/// Administrators only.
/// </summary>
public sealed record DeleteClassCommand(Guid Id) : IRequest<Unit>;
