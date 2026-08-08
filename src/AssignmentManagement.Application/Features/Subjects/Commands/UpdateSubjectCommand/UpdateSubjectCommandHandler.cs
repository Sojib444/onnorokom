using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Exceptions;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Subjects;

public sealed class UpdateSubjectCommandHandler : IRequestHandler<UpdateSubjectCommand, SubjectResponse>
{
    private readonly ISubjectWriteRepository _subjects;
    private readonly ISubjectReadRepository _subjectLookups;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateSubjectCommandHandler(
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

    public async Task<SubjectResponse> Handle(UpdateSubjectCommand request, CancellationToken cancellationToken)
    {
        var entity = await _subjects.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Subject>(request.Id);

        if (await _subjectLookups.ExistsByCodeAsync(request.Code, cancellationToken)
            && !string.Equals(entity.Code, request.Code, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleViolation($"A subject with code '{request.Code}' already exists.");
        }

        entity.Update(request.Name, request.Code);
        _subjects.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SubjectResponse>(entity);
    }
}
