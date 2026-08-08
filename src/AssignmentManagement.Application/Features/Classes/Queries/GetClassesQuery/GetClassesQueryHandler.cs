using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Contracts;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Classes;

public sealed class GetClassesQueryHandler : IRequestHandler<GetClassesQuery, IReadOnlyList<ClassResponse>>
{
    private readonly IClassReadRepository _classes;
    private readonly IMapper _mapper;

    public GetClassesQueryHandler(IClassReadRepository classes, IMapper mapper)
    {
        _classes = classes;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ClassResponse>> Handle(
        GetClassesQuery request,
        CancellationToken cancellationToken) =>
        _mapper.Map<List<ClassResponse>>(await _classes.GetAllAsync(cancellationToken));
}
