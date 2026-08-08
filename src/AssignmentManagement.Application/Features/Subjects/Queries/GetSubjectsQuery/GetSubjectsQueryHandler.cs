using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Contracts;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Subjects;

public sealed class GetSubjectsQueryHandler : IRequestHandler<GetSubjectsQuery, IReadOnlyList<SubjectResponse>>
{
    private readonly ISubjectReadRepository _subjects;
    private readonly IMapper _mapper;

    public GetSubjectsQueryHandler(ISubjectReadRepository subjects, IMapper mapper)
    {
        _subjects = subjects;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<SubjectResponse>> Handle(
        GetSubjectsQuery request,
        CancellationToken cancellationToken) =>
        _mapper.Map<List<SubjectResponse>>(await _subjects.GetAllAsync(cancellationToken));
}
