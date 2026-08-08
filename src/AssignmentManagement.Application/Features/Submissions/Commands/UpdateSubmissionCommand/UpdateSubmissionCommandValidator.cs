using FluentValidation;

namespace AssignmentManagement.Application.Features.Submissions;

public sealed class UpdateSubmissionCommandValidator : AbstractValidator<UpdateSubmissionCommand>
{
    public UpdateSubmissionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Answer).NotEmpty().MaximumLength(8000);
    }
}
