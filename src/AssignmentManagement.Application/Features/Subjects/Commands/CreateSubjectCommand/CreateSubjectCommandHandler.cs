using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Exceptions;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Subjects;

public sealed class CreateSubjectCommandHandler : IRequestHandler<CreateSubjectCommand, SubjectResponse>
{
    private readonly ISubjectWriteRepository _subjects;
    private readonly ISubjectReadRepository _subjectLookups;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateSubjectCommandHandler(
        ISubjectWriteRepository subjects,
        ISubjectReadRepository subjectLookups,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _subjects = subjects;
        _subjectLookups = subjectLookups;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<SubjectResponse> Handle(CreateSubjectCommand request, CancellationToken cancellationToken)
    {
        if (await _subjectLookups.ExistsByCodeAsync(request.Code, cancellationToken))
        {
            throw new BusinessRuleViolation($"A subject with code '{request.Code}' already exists.");
        }

        var entity = new Subject(request.Name, request.Code);
        _subjects.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SubjectResponse>(entity);
    }
}
