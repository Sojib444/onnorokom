using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Exceptions;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Classes;

public sealed class CreateClassCommandHandler : IRequestHandler<CreateClassCommand, ClassResponse>
{
    private readonly IClassWriteRepository _classes;
    private readonly IClassReadRepository _classLookups;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateClassCommandHandler(
        IClassWriteRepository classes,
        IClassReadRepository classLookups,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _classes = classes;
        _classLookups = classLookups;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ClassResponse> Handle(CreateClassCommand request, CancellationToken cancellationToken)
    {
        if (await _classLookups.ExistsByNameAsync(request.Name, cancellationToken))
        {
            throw new BusinessRuleViolation($"A class named '{request.Name}' already exists.");
        }

        var entity = new Class(request.Name, request.Description);
        _classes.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ClassResponse>(entity);
    }
}
