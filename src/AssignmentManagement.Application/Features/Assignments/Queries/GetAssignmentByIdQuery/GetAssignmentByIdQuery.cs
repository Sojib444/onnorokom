using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Assignments;

/// <summary>Returns a single assignment if the caller is allowed to see it.</summary>
public sealed record GetAssignmentByIdQuery(Guid Id) : IRequest<AssignmentResponse>;
