using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Mapping;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Users;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserResponse>>
{
    private readonly IUserReadRepository _users;
    private readonly IClassReadRepository _classes;
    private readonly IMapper _mapper;

    public GetUsersQueryHandler(
        IUserReadRepository users,
        IClassReadRepository classes,
        IMapper mapper)
    {
        _users = users;
        _classes = classes;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<UserResponse>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var classNames = (await _classes.GetAllAsync(cancellationToken))
            .ToDictionary(c => c.Id, c => c.Name);

        return _mapper.Map<List<UserResponse>>(
            await _users.GetAllAsync(cancellationToken),
            options => options.Items[MapperContext.ClassNames] = classNames);
    }
}
