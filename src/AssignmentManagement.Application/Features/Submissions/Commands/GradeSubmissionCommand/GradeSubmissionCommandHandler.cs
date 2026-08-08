using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Mapping;
using AssignmentManagement.Domain.Entities;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Submissions;

public sealed class GradeSubmissionCommandHandler : IRequestHandler<GradeSubmissionCommand, SubmissionResponse>
{
    private readonly ISubmissionWriteRepository _submissions;
    private readonly IAssignmentReadRepository _assignments;
    private readonly IUserReadRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GradeSubmissionCommandHandler(
        ISubmissionWriteRepository submissions,
        IAssignmentReadRepository assignments,
        IUserReadRepository users,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _submissions = submissions;
        _assignments = assignments;
        _users = users;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<SubmissionResponse> Handle(
        GradeSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await _submissions.GetByIdWithAttachmentsAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Submission>(request.Id);

        var assignment = await _assignments.GetByIdAsync(submission.AssignmentId, cancellationToken)
            ?? throw NotFoundException.For<Assignment>(submission.AssignmentId);

        if (assignment.TeacherId != _currentUser.UserId)
        {
            throw new ForbiddenException("You can only grade submissions for your own assignments.");
        }

        submission.Grade(
            _currentUser.UserId!.Value,
            assignment.TeacherId,
            assignment.MaximumMarks,
            request.Marks,
            request.Feedback,
            DateTimeOffset.UtcNow);
        _submissions.Update(submission);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var student = await _users.GetByIdAsync(submission.StudentId, cancellationToken);

        return _mapper.Map<SubmissionResponse>(submission, options =>
        {
            options.Items[MapperContext.AssignmentTitle] = assignment.Title;
            options.Items[MapperContext.StudentName] = student?.FullName;
        });
    }
}
