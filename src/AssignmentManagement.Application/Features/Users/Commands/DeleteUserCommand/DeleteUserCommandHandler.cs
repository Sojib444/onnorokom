using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.Exceptions;
using MediatR;

namespace AssignmentManagement.Application.Features.Users;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IUserWriteRepository _users;
    private readonly IAssignmentReadRepository _assignments;
    private readonly ISubmissionReadRepository _submissions;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserCommandHandler(
        IUserWriteRepository users,
        IAssignmentReadRepository assignments,
        ISubmissionReadRepository submissions,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _assignments = assignments;
        _submissions = submissions;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<User>(request.Id);

        if (user.Role == UserRole.Teacher
            && await _assignments.ExistsForTeacherAsync(user.Id, cancellationToken))
        {
            throw new BusinessRuleViolation(
                "This teacher has authored assignments and cannot be deleted.");
        }

        if (user.Role == UserRole.Student
            && await _submissions.ExistsForStudentAsync(user.Id, cancellationToken))
        {
            throw new BusinessRuleViolation(
                "This student has submissions and cannot be deleted.");
        }

        _users.Remove(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
