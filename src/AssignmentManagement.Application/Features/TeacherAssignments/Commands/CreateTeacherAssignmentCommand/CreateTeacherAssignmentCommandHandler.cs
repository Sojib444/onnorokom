using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Mapping;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Domain.Exceptions;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.TeacherAssignments;

public sealed class CreateTeacherAssignmentCommandHandler
    : IRequestHandler<CreateTeacherAssignmentCommand, TeacherAssignmentResponse>
{
    private readonly ITeacherAssignmentWriteRepository _allocations;
    private readonly ITeacherAssignmentReadRepository _allocationLookups;
    private readonly IUserReadRepository _users;
    private readonly IClassReadRepository _classes;
    private readonly ISubjectReadRepository _subjects;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateTeacherAssignmentCommandHandler(
        ITeacherAssignmentWriteRepository allocations,
        ITeacherAssignmentReadRepository allocationLookups,
        IUserReadRepository users,
        IClassReadRepository classes,
        ISubjectReadRepository subjects,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _allocations = allocations;
        _allocationLookups = allocationLookups;
        _users = users;
        _classes = classes;
        _subjects = subjects;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TeacherAssignmentResponse> Handle(
        CreateTeacherAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _users.GetByIdAsync(request.TeacherId, cancellationToken)
            ?? throw NotFoundException.For<User>(request.TeacherId);

        // Only Teacher-role users may be allocated. This check lives in the handler
        // (not the domain, which has no User aggregate reference here) and is what
        // prevents an allocation from ever authoring or grading as a non-teacher.
        if (teacher.Role != UserRole.Teacher)
        {
            throw new BusinessRuleViolation("Only users with the Teacher role can be allocated to teach.");
        }

        var klass = await _classes.GetByIdAsync(request.ClassId, cancellationToken)
            ?? throw NotFoundException.For<Class>(request.ClassId);

        var subject = await _subjects.GetByIdAsync(request.SubjectId, cancellationToken)
            ?? throw NotFoundException.For<Subject>(request.SubjectId);

        if (await _allocationLookups.ExistsForTeacherAsync(
                request.TeacherId,
                request.ClassId,
                request.SubjectId,
                cancellationToken))
        {
            throw new BusinessRuleViolation(
                "This teacher is already allocated to the class and subject pair.");
        }

        var entity = new Domain.Entities.TeacherAssignment(
            request.TeacherId,
            request.ClassId,
            request.SubjectId);
        _allocations.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<TeacherAssignmentResponse>(entity, options =>
        {
            options.Items[MapperContext.TeacherNames] =
                new Dictionary<Guid, string> { [entity.TeacherId] = teacher.FullName };
            options.Items[MapperContext.ClassNames] =
                new Dictionary<Guid, string> { [entity.ClassId] = klass.Name };
            options.Items[MapperContext.SubjectNames] =
                new Dictionary<Guid, string> { [entity.SubjectId] = subject.Name };
        });
    }
}
