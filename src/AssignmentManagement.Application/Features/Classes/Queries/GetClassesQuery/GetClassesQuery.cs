using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Classes;

/// <summary>Returns all classes ordered by name. Any authenticated user.</summary>
public sealed record GetClassesQuery : IRequest<IReadOnlyList<ClassResponse>>;
