using FluentValidation;
using SkillIssue.Http.Errors;

namespace SkillIssue.Modules.Users;

public sealed class UpdateSocialLinksRequestValidator : AbstractValidator<UpdateSocialLinksRequest>
{
    public UpdateSocialLinksRequestValidator()
    {
        RuleFor(request => request.Links)
            .NotNull()
            .Must(links =>
                links is not null
                && links.Length <= 20
                && links.All(link => link is not null)
                && links
                    .Select(link => link.Network?.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() == links.Length
            );

        RuleForEach(request => request.Links)
            .NotNull()
            .ChildRules(link =>
            {
                link.RuleFor(value => value.Network).NotEmpty().MaximumLength(80);
                link.RuleFor(value => value.Url)
                    .Must(LinkValidation.IsValidLink)
                    .WithMessage(ErrorMessages.InvalidField);
            });
    }
}
