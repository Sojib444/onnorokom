using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Mapping;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

public sealed class GetAssignmentSubmissionsQueryHandler
    : IRequestHandler<GetAssignmentSubmissionsQuery, IReadOnlyList<SubmissionResponse>>
{
    private readonly ISubmissionReadRepository _submissions;
    private readonly IAssignmentReadRepository _assignments;
    private readonly IUserReadRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public GetAssignmentSubmissionsQueryHandler(
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
        GetAssignmentSubmissionsQuery request,
        CancellationToken cancellationToken)
    {
        var assignment = await _assignments.GetByIdAsync(request.AssignmentId, cancellationToken)
            ?? throw NotFoundException.For<Assignment>(request.AssignmentId);

        if (_currentUser.Role == UserRole.Teacher && assignment.TeacherId != _currentUser.UserId)
        {
            throw new ForbiddenException("You can only view submissions for your own assignments.");
        }

        var studentNames = (await _users.GetAllAsync(cancellationToken))
            .ToDictionary(u => u.Id, u => u.FullName);

        var submissions = await _submissions.GetByAssignmentAsync(
            request.AssignmentId, cancellationToken);

        return submissions
            .Select(s => _mapper.Map<SubmissionResponse>(s, options =>
            {
                options.Items[MapperContext.AssignmentTitle] = assignment.Title;
                options.Items[MapperContext.StudentName] =
                    studentNames.GetValueOrDefault(s.StudentId);
            }))
            .ToList();
    }
}
