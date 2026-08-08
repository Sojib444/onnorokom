using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Classes;

/// <summary>Updates a class. Administrators only.</summary>
public sealed record UpdateClassCommand(Guid Id, string Name, string? Description) : IRequest<ClassResponse>;
