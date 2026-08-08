using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Mapping;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.TeacherAssignments;

public sealed class GetTeacherAssignmentsQueryHandler
    : IRequestHandler<GetTeacherAssignmentsQuery, IReadOnlyList<TeacherAssignmentResponse>>
{
    private readonly ITeacherAssignmentReadRepository _allocations;
    private readonly IUserReadRepository _users;
    private readonly IClassReadRepository _classes;
    private readonly ISubjectReadRepository _subjects;
    private readonly IMapper _mapper;

    public GetTeacherAssignmentsQueryHandler(
        ITeacherAssignmentReadRepository allocations,
        IUserReadRepository users,
        IClassReadRepository classes,
        ISubjectReadRepository subjects,
        IMapper mapper)
    {
        _allocations = allocations;
        _users = users;
        _classes = classes;
        _subjects = subjects;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<TeacherAssignmentResponse>> Handle(
        GetTeacherAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var teachers = (await _users.GetAllAsync(cancellationToken))
            .ToDictionary(u => u.Id, u => u.FullName);
        var classes = (await _classes.GetAllAsync(cancellationToken))
            .ToDictionary(c => c.Id, c => c.Name);
        var subjects = (await _subjects.GetAllAsync(cancellationToken))
            .ToDictionary(s => s.Id, s => s.Name);

        return _mapper.Map<List<TeacherAssignmentResponse>>(
            await _allocations.GetAllAsync(cancellationToken),
            options =>
            {
                options.Items[MapperContext.TeacherNames] = teachers;
                options.Items[MapperContext.ClassNames] = classes;
                options.Items[MapperContext.SubjectNames] = subjects;
            });
    }
}
