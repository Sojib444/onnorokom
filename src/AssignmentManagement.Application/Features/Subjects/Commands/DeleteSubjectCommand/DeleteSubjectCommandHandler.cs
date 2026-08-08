using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Exceptions;
using MediatR;

namespace AssignmentManagement.Application.Features.Subjects;

public sealed class DeleteSubjectCommandHandler : IRequestHandler<DeleteSubjectCommand, Unit>
{
    private readonly ISubjectWriteRepository _subjects;
    private readonly IAssignmentReadRepository _assignments;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSubjectCommandHandler(
        ISubjectWriteRepository subjects,
        IAssignmentReadRepository assignments,
        IUnitOfWork unitOfWork)
    {
        _subjects = subjects;
        _assignments = assignments;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteSubjectCommand request, CancellationToken cancellationToken)
    {
        var entity = await _subjects.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Subject>(request.Id);

        if (await _assignments.ExistsForSubjectAsync(request.Id, cancellationToken))
        {
            throw new BusinessRuleViolation("Assignments reference this subject and it cannot be deleted.");
        }

        _subjects.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
