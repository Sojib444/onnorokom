using FluentValidation;

namespace AssignmentManagement.Application.Features.TeacherAssignments;

public sealed class CreateTeacherAssignmentCommandValidator : AbstractValidator<CreateTeacherAssignmentCommand>
{
    public CreateTeacherAssignmentCommandValidator()
    {
        RuleFor(x => x.TeacherId).NotEmpty();
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
    }
}
