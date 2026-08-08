using AssignmentManagement.Domain.Enums;
using FluentValidation;

namespace AssignmentManagement.Application.Features.Users;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => Enum.TryParse<UserRole>(role, true, out _))
            .WithMessage("Role must be one of: Admin, Teacher, Student.");

        RuleFor(x => x.ClassId)
            .NotNull()
            .When(x => string.Equals(x.Role, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase))
            .WithMessage("A class is required for students.");
    }
}
