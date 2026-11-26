using FluentValidation;
using SkillIssue.Modules.Users.Models;

namespace SkillIssue.Modules.Users.Validators;

public sealed class SignInRequestValidator : AbstractValidator<SignInRequest>
{
    public SignInRequestValidator()
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
            .MaximumLength(128)
            .WithErrorCode("too_long")
            .WithName("Password");
    }
}
