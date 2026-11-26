using FluentValidation;
using SkillIssue.Modules.Users.Models;

namespace SkillIssue.Modules.Users.Validators;

public sealed class SignUpRequestValidator : AbstractValidator<SignUpRequest>
{
    public SignUpRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Email)
            .NotEmpty()
            .WithErrorCode("required")
            .MaximumLength(254)
            .WithErrorCode("too_long")
            .EmailAddress()
            .WithErrorCode("invalid_email")
            .WithName("Email");

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithErrorCode("required")
            .Length(15, 128)
            .WithErrorCode("invalid_length")
            .Must(password => password is not null && !password.Any(char.IsWhiteSpace))
            .WithErrorCode("invalid")
            .WithName("Password");

        RuleFor(request => request.FullName)
            .NotEmpty()
            .WithErrorCode("required")
            .MaximumLength(200)
            .WithErrorCode("too_long")
            .WithName("Full name");
    }
}
