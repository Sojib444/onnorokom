using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Mapping;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.TeacherAssignments;

public sealed class GetMyTeacherAssignmentsQueryHandler
    : IRequestHandler<GetMyTeacherAssignmentsQuery, IReadOnlyList<TeacherAssignmentResponse>>
{
    private readonly ITeacherAssignmentReadRepository _allocations;
    private readonly IUserReadRepository _users;
    private readonly IClassReadRepository _classes;
    private readonly ISubjectReadRepository _subjects;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public GetMyTeacherAssignmentsQueryHandler(
        ITeacherAssignmentReadRepository allocations,
        IUserReadRepository users,
        IClassReadRepository classes,
        ISubjectReadRepository subjects,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _allocations = allocations;
        _users = users;
        _classes = classes;
        _subjects = subjects;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<TeacherAssignmentResponse>> Handle(
        GetMyTeacherAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.UserId!.Value;
        var classes = (await _classes.GetAllAsync(cancellationToken))
            .ToDictionary(c => c.Id, c => c.Name);
        var subjects = (await _subjects.GetAllAsync(cancellationToken))
            .ToDictionary(s => s.Id, s => s.Name);
        var teacher = await _users.GetByIdAsync(teacherId, cancellationToken);

        var allocations = await _allocations.GetByTeacherAsync(teacherId, cancellationToken);

        var teacherNames = new Dictionary<Guid, string>
        {
            [teacherId] = teacher?.FullName ?? "You",
        };

        return _mapper.Map<List<TeacherAssignmentResponse>>(
            allocations,
            options =>
            {
                options.Items[MapperContext.TeacherNames] = teacherNames;
                options.Items[MapperContext.ClassNames] = classes;
                options.Items[MapperContext.SubjectNames] = subjects;
            });
    }
}
