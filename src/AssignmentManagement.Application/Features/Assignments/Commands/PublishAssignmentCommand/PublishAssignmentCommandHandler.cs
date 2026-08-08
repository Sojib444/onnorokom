using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using MediatR;

namespace AssignmentManagement.Application.Features.Assignments;

public sealed class PublishAssignmentCommandHandler : IRequestHandler<PublishAssignmentCommand, Unit>
{
    private readonly IAssignmentWriteRepository _assignments;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public PublishAssignmentCommandHandler(
        IAssignmentWriteRepository assignments,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _assignments = assignments;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(PublishAssignmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _assignments.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Domain.Entities.Assignment>(request.Id);

        if (entity.TeacherId != _currentUser.UserId)
        {
            throw new ForbiddenException("You can only publish your own assignments.");
        }

        entity.Publish(DateTimeOffset.UtcNow);
        _assignments.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
