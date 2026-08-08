using AssignmentManagement.Application.Contracts;
using MediatR;

namespace AssignmentManagement.Application.Features.Subjects;

/// <summary>Creates a subject. Administrators only.</summary>
public sealed record CreateSubjectCommand(string Name, string Code) : IRequest<SubjectResponse>;
