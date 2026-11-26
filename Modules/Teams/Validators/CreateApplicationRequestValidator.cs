using FluentValidation;
using SkillIssue.Modules.Teams.Models;

namespace SkillIssue.Modules.Teams.Validators;

public sealed class CreateApplicationRequestValidator : AbstractValidator<CreateApplicationRequest>
{
    public CreateApplicationRequestValidator()
    {
        RuleFor(request => request.TeamId).GreaterThan(0);
        RuleFor(request => request.PositionId).GreaterThan(0);
        RuleFor(request => request.CoverLetter)
            .MaximumLength(2000)
            .Must(letter => letter?.Contains('\0') != true);
    }
}
