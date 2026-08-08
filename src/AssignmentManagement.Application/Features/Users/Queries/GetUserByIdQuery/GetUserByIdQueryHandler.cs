using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Mapping;
using AssignmentManagement.Domain.Entities;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Users;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserResponse>
{
    private readonly IUserReadRepository _users;
    private readonly IClassReadRepository _classes;
    private readonly IMapper _mapper;

    public GetUserByIdQueryHandler(
        IUserReadRepository users,
        IClassReadRepository classes,
        IMapper mapper)
    {
        _users = users;
        _classes = classes;
        _mapper = mapper;
    }

    public async Task<UserResponse> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<User>(request.Id);

        var classNames = (await _classes.GetAllAsync(cancellationToken))
            .ToDictionary(c => c.Id, c => c.Name);

        return _mapper.Map<UserResponse>(
            user,
            options => options.Items[MapperContext.ClassNames] = classNames);
    }
}
