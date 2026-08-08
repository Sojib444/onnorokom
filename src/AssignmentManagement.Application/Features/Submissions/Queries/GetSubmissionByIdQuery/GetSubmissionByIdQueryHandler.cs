using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Mapping;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

public sealed class GetSubmissionByIdQueryHandler : IRequestHandler<GetSubmissionByIdQuery, SubmissionResponse>
{
    private readonly ISubmissionReadRepository _submissions;
    private readonly IAssignmentReadRepository _assignments;
    private readonly IUserReadRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public GetSubmissionByIdQueryHandler(
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

    public async Task<SubmissionResponse> Handle(GetSubmissionByIdQuery request, CancellationToken cancellationToken)
    {
        var submission = await _submissions.GetByIdWithAttachmentsAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Submission>(request.Id);

        var assignment = await _assignments.GetByIdAsync(submission.AssignmentId, cancellationToken);

        EnsureCanAccess(submission, assignment);

        var student = await _users.GetByIdAsync(submission.StudentId, cancellationToken);

        return _mapper.Map<SubmissionResponse>(submission, options =>
        {
            options.Items[MapperContext.AssignmentTitle] = assignment?.Title;
            options.Items[MapperContext.StudentName] = student?.FullName;
        });
    }

    /// <summary>
    /// Enforces the role-specific access rule for a single submission.
    /// </summary>
    /// <remarks>
    /// Role membership alone is insufficient: a teacher may only access submissions for
    /// assignments they own (the assignment must still exist to prove ownership), while
    /// a student may only access their own submissions. Identity comes exclusively from
    /// the validated JWT via <see cref="ICurrentUser"/>, never from the request.
    /// </remarks>
    private void EnsureCanAccess(Submission submission, Assignment? assignment)
    {
        switch (_currentUser.Role)
        {
            case UserRole.Admin:
                return;
            case UserRole.Teacher:
                if (assignment is null || assignment.TeacherId != _currentUser.UserId)
                {
                    throw new ForbiddenException("You can only view submissions for your own assignments.");
                }
                return;
            default:
                if (submission.StudentId != _currentUser.UserId)
                {
                    throw new ForbiddenException("You can only view your own submissions.");
                }
                return;
        }
    }
}
