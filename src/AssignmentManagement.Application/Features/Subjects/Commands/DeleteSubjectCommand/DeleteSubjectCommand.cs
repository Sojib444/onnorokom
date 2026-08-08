using MediatR;

namespace AssignmentManagement.Application.Features.Subjects;

/// <summary>
/// Deletes a subject. A subject referenced by any teacher allocation is deleted along
/// with the allocation, but one used by an assignment cannot be deleted because the
/// assignment must keep its subject. Administrators only.
/// </summary>
public sealed record DeleteSubjectCommand(Guid Id) : IRequest<Unit>;
