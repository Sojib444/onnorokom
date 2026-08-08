using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Mapping;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

public sealed class GetMySubmissionsQueryHandler
    : IRequestHandler<GetMySubmissionsQuery, IReadOnlyList<SubmissionResponse>>
{
    private readonly ISubmissionReadRepository _submissions;
    private readonly IAssignmentReadRepository _assignments;
    private readonly IUserReadRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public GetMySubmissionsQueryHandler(
        ISubmissionReadRepository submissions,
        IAssignmentReadRepository assignments,
        IUserReadRepository users,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _submissions = submissions;
        _assignments = assignments;
        _users = users;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<SubmissionResponse>> Handle(
        GetMySubmissionsQuery request,
        CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId!.Value;

        var titles = (await _assignments.GetAllAsync(cancellationToken))
            .ToDictionary(a => a.Id, a => a.Title);

        var student = await _users.GetByIdAsync(studentId, cancellationToken);

        var submissions = await _submissions.GetByStudentAsync(studentId, cancellationToken);

        return submissions
            .Select(s => _mapper.Map<SubmissionResponse>(s, options =>
            {
                options.Items[MapperContext.AssignmentTitle] =
                    titles.GetValueOrDefault(s.AssignmentId);
                options.Items[MapperContext.StudentName] = student?.FullName;
            }))
            .ToList();
    }
}
