using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Exceptions;
using MediatR;

namespace AssignmentManagement.Application.Features.Classes;

public sealed class DeleteClassCommandHandler : IRequestHandler<DeleteClassCommand, Unit>
{
    private readonly IClassWriteRepository _classes;
    private readonly IAssignmentReadRepository _assignments;
    private readonly IUserReadRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteClassCommandHandler(
        IClassWriteRepository classes,
        IAssignmentReadRepository assignments,
        IUserReadRepository users,
        IUnitOfWork unitOfWork)
    {
        _classes = classes;
        _assignments = assignments;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteClassCommand request, CancellationToken cancellationToken)
    {
        var entity = await _classes.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Class>(request.Id);

        if (await _assignments.ExistsForClassAsync(request.Id, cancellationToken))
        {
            throw new BusinessRuleViolation("Assignments reference this class and it cannot be deleted.");
        }

        if (await _users.ExistsStudentInClassAsync(request.Id, cancellationToken))
        {
            throw new BusinessRuleViolation("Students are enrolled in this class and it cannot be deleted.");
        }

        _classes.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
