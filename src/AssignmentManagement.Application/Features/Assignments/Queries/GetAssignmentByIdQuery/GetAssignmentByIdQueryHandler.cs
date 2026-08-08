using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Domain.Enums;
using AutoMapper;
using MediatR;

namespace AssignmentManagement.Application.Features.Assignments;

public sealed class GetAssignmentByIdQueryHandler : IRequestHandler<GetAssignmentByIdQuery, AssignmentResponse>
{
    private readonly IAssignmentReadRepository _assignments;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public GetAssignmentByIdQueryHandler(
        IAssignmentReadRepository assignments,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _assignments = assignments;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<AssignmentResponse> Handle(
        GetAssignmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var assignment = await _assignments.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Domain.Entities.Assignment>(request.Id);

        EnsureVisible(assignment);

        return _mapper.Map<AssignmentResponse>(assignment);
    }

    /// <summary>
    /// Enforces the role-specific visibility rule for a single assignment.
    /// </summary>
    /// <remarks>
    /// This is resource-based authorization, not just role membership: an admin sees
    /// everything, a teacher only their own assignments, and a student only published
    /// assignments for their own class. Identity comes exclusively from the validated
    /// JWT via <see cref="ICurrentUser"/>, never from the request.
    /// </remarks>
    private void EnsureVisible(Domain.Entities.Assignment assignment)
    {
        switch (_currentUser.Role)
        {
            case UserRole.Admin:
                return;
            case UserRole.Teacher:
                if (assignment.TeacherId != _currentUser.UserId)
                {
                    throw new ForbiddenException("You can only view your own assignments.");
                }
                return;
            default:
                if (assignment.ClassId != _currentUser.ClassId
                    || assignment.Status != AssignmentStatus.Published)
                {
                    throw new ForbiddenException("You can only view published assignments for your class.");
                }
                return;
        }
    }
}
