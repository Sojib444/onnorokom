using FluentValidation;

namespace AssignmentManagement.Application.Features.Submissions;

public sealed class CreateSubmissionCommandValidator : AbstractValidator<CreateSubmissionCommand>
{
    public CreateSubmissionCommandValidator()
    {
        RuleFor(x => x.AssignmentId).NotEmpty();
        RuleFor(x => x.Answer).NotEmpty().MaximumLength(8000);
    }
}
