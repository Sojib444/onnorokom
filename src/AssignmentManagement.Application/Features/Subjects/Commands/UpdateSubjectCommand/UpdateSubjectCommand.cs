using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Subjects;

/// <summary>Updates a subject. Administrators only.</summary>
public sealed record UpdateSubjectCommand(Guid Id, string Name, string Code) : IRequest<SubjectResponse>;
