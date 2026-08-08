using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using MediatR;

namespace AssignmentManagement.Application.Features.TeacherAssignments;

public sealed class DeleteTeacherAssignmentCommandHandler
    : IRequestHandler<DeleteTeacherAssignmentCommand, Unit>
{
    private readonly ITeacherAssignmentWriteRepository _allocations;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTeacherAssignmentCommandHandler(
        ITeacherAssignmentWriteRepository allocations,
        IUnitOfWork unitOfWork)
    {
        _allocations = allocations;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteTeacherAssignmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _allocations.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Domain.Entities.TeacherAssignment>(request.Id);

        _allocations.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
