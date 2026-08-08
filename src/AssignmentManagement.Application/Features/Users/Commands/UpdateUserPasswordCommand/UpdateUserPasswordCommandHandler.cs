using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Domain.Entities;
using MediatR;

namespace AssignmentManagement.Application.Features.Users;

public sealed class UpdateUserPasswordCommandHandler
    : IRequestHandler<UpdateUserPasswordCommand, Unit>
{
    private readonly IUserWriteRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserPasswordCommandHandler(
        IUserWriteRepository users,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<User>(request.Id);

        user.SetPasswordHash(_passwordHasher.Hash(request.Password), DateTimeOffset.UtcNow);
        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
