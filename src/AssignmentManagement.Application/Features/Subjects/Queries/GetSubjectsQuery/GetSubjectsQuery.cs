using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Subjects;

/// <summary>Returns all subjects ordered by name. Any authenticated user.</summary>
public sealed record GetSubjectsQuery : IRequest<IReadOnlyList<SubjectResponse>>;
