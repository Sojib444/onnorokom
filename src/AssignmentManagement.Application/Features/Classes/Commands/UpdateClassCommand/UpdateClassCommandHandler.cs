using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Exceptions;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Classes;

public sealed class UpdateClassCommandHandler : IRequestHandler<UpdateClassCommand, ClassResponse>
{
    private readonly IClassWriteRepository _classes;
    private readonly IClassReadRepository _classLookups;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateClassCommandHandler(
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

    public async Task<ClassResponse> Handle(UpdateClassCommand request, CancellationToken cancellationToken)
    {
        var entity = await _classes.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Class>(request.Id);

        if (await _classLookups.ExistsByNameAsync(request.Name, cancellationToken)
            && !string.Equals(entity.Name, request.Name, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleViolation($"A class named '{request.Name}' already exists.");
        }

        entity.Update(request.Name, request.Description);
        _classes.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ClassResponse>(entity);
    }
}
