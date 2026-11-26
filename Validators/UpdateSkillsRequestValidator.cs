using FluentValidation;
using SkillIssue.Models.Requests;

namespace SkillIssue.Validators;

public sealed class UpdateSkillsRequestValidator : AbstractValidator<UpdateSkillRequest[]>
{
    public UpdateSkillsRequestValidator()
    {
        RuleFor(skills => skills)
            .Must(skills =>
                skills.Length <= 20
                && skills.All(skill =>
                    skill is not null && skill.SkillId > 0 && skill.SkillLevel is >= 1 and <= 3
                )
                && skills.Select(skill => skill.SkillId).Distinct().Count() == skills.Length
            );
    }
}
