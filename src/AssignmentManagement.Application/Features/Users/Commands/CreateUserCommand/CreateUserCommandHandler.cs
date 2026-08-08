using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.Exceptions;
using AssignmentManagement.Domain.ValueObjects;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Users;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserResponse>
{
    private readonly IUserWriteRepository _users;
    private readonly IUserReadRepository _userLookups;
    private readonly IClassReadRepository _classes;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateUserCommandHandler(
        IUserWriteRepository users,
        IUserReadRepository userLookups,
        IClassReadRepository classes,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _users = users;
        _userLookups = userLookups;
        _classes = classes;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userLookups.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new BusinessRuleViolation("A user with this email already exists.");
        }

        var role = Enum.Parse<UserRole>(request.Role, ignoreCase: true);

        if (role == UserRole.Student && request.ClassId is Guid classId
            && !await _classes.ExistsAsync(classId, cancellationToken))
        {
            throw NotFoundException.For<Class>(classId);
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User(request.FullName, new EmailAddress(request.Email), role, request.ClassId, now);
        user.SetPasswordHash(_passwordHasher.Hash(request.Password), now);

        _users.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserResponse>(user);
    }
}
