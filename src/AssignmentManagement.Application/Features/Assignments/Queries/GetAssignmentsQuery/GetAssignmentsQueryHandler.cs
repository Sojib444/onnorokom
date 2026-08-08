using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Domain.Enums;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Assignments;

public sealed class GetAssignmentsQueryHandler : IRequestHandler<GetAssignmentsQuery, IReadOnlyList<AssignmentResponse>>
{
    private readonly IAssignmentReadRepository _assignments;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public GetAssignmentsQueryHandler(
        IAssignmentReadRepository assignments,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _assignments = assignments;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<AssignmentResponse>> Handle(
        GetAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Entities.Assignment> assignments = _currentUser.Role switch
        {
            UserRole.Admin => await _assignments.GetAllAsync(cancellationToken),
            UserRole.Teacher => await _assignments.GetByTeacherAsync(
                _currentUser.UserId!.Value,
                cancellationToken),
            _ => await _assignments.GetPublishedForClassAsync(
                _currentUser.ClassId!.Value,
                cancellationToken),
        };

        return _mapper.Map<List<AssignmentResponse>>(assignments);
    }
}
