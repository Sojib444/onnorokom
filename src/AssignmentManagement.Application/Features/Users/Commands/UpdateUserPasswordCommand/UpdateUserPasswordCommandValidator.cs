using FluentValidation;

namespace AssignmentManagement.Application.Features.Users;

public sealed class UpdateUserPasswordCommandValidator : AbstractValidator<UpdateUserPasswordCommand>
{
    public UpdateUserPasswordCommandValidator()
    {
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
