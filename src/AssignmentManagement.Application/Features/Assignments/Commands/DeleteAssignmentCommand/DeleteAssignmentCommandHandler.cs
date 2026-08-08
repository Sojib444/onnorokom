using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using MediatR;

namespace AssignmentManagement.Application.Features.Assignments;

public sealed class DeleteAssignmentCommandHandler : IRequestHandler<DeleteAssignmentCommand, Unit>
{
    private readonly IAssignmentWriteRepository _assignments;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAssignmentCommandHandler(
        IAssignmentWriteRepository assignments,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _assignments = assignments;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteAssignmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _assignments.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Domain.Entities.Assignment>(request.Id);

        if (entity.TeacherId != _currentUser.UserId)
        {
            throw new ForbiddenException("You can only delete your own assignments.");
        }

        entity.EnsureCanBeDeleted();

        _assignments.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
