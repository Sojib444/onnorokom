using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Mapping;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Exceptions;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Assignments;

public sealed class CreateAssignmentCommandHandler : IRequestHandler<CreateAssignmentCommand, AssignmentResponse>
{
    private readonly IAssignmentWriteRepository _assignments;
    private readonly ITeacherAssignmentReadRepository _allocations;
    private readonly IClassReadRepository _classes;
    private readonly ISubjectReadRepository _subjects;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateAssignmentCommandHandler(
        IAssignmentWriteRepository assignments,
        ITeacherAssignmentReadRepository allocations,
        IClassReadRepository classes,
        ISubjectReadRepository subjects,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _assignments = assignments;
        _allocations = allocations;
        _classes = classes;
        _subjects = subjects;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<AssignmentResponse> Handle(
        CreateAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var klass = await _classes.GetByIdAsync(request.ClassId, cancellationToken)
            ?? throw NotFoundException.For<Class>(request.ClassId);

        var subject = await _subjects.GetByIdAsync(request.SubjectId, cancellationToken)
            ?? throw NotFoundException.For<Subject>(request.SubjectId);

        var teacherId = _currentUser.UserId!.Value;

        if (!await _allocations.ExistsForTeacherAsync(
                teacherId,
                request.ClassId,
                request.SubjectId,
                cancellationToken))
        {
            throw new BusinessRuleViolation(
                "You are not allocated to teach this class and subject.");
        }

        var entity = new Domain.Entities.Assignment(
            teacherId,
            request.ClassId,
            request.SubjectId,
            request.Title,
            request.Description,
            request.Deadline,
            request.MaximumMarks,
            DateTimeOffset.UtcNow);

        _assignments.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<AssignmentResponse>(entity, options =>
        {
            options.Items[MapperContext.ClassName] = klass.Name;
            options.Items[MapperContext.SubjectName] = subject.Name;
        });
    }
}
