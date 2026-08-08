using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Users;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserResponse>
{
    private readonly IUserWriteRepository _users;
    private readonly IClassReadRepository _classes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateUserCommandHandler(
        IUserWriteRepository users,
        IClassReadRepository classes,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _users = users;
        _classes = classes;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<User>(request.Id);

        if (user.Role == UserRole.Student && request.ClassId is Guid classId
            && !await _classes.ExistsAsync(classId, cancellationToken))
        {
            throw NotFoundException.For<Class>(classId);
        }

        user.UpdateProfile(request.FullName, request.ClassId, DateTimeOffset.UtcNow);
        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserResponse>(user);
    }
}
