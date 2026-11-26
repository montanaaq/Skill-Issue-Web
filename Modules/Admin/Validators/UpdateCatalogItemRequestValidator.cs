using FluentValidation;
using SkillIssue.Modules.Admin.Models;

namespace SkillIssue.Modules.Admin.Validators;

public sealed class UpdateCatalogItemRequestValidator : AbstractValidator<UpdateCatalogItemRequest>
{
    public UpdateCatalogItemRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
        RuleFor(request => request.IsActive).NotNull().WithErrorCode("required");
    }
}
