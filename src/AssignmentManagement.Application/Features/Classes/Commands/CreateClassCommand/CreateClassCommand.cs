using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Classes;

/// <summary>Creates a class. Administrators only.</summary>
public sealed record CreateClassCommand(string Name, string? Description) : IRequest<ClassResponse>;
