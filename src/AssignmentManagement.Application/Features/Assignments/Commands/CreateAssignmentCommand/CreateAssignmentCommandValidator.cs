using FluentValidation;

namespace AssignmentManagement.Application.Features.Assignments;

public sealed class CreateAssignmentCommandValidator : AbstractValidator<CreateAssignmentCommand>
{
    public CreateAssignmentCommandValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Deadline).GreaterThan(DateTimeOffset.UtcNow);
        RuleFor(x => x.MaximumMarks).GreaterThan(0);
    }
}
