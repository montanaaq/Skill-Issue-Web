using System.Net;
using FluentValidation;
using SkillIssue.Http;
using SkillIssue.Http.Errors;
using SkillIssue.Modules.Admin.Models;
using SkillIssue.Modules.Admin.Repositories;
using SkillIssue.Modules.Admin.Validators;

namespace SkillIssue.Modules.Admin;

public sealed class AdminController(AdminCatalogRepository repository)
{
    private readonly UpdateCatalogItemRequestValidator _catalogItemValidator = new();

    public async Task CatalogAsync(RequestContext context)
    {
        var items = await repository.GetAsync(context.CancellationToken);
        await context.WriteJsonAsync(
            items.Select(item => new
            {
                kind = item.Kind,
                id = item.Id,
                name = item.Name,
                isActive = item.IsActive,
            })
        );
    }

    public async Task UpdateCatalogItemAsync(RequestContext context, string kind)
    {
        _ = int.TryParse(context.RouteValues["id"], out int id);
        var body = await context.ReadJsonAsync<UpdateCatalogItemRequest>();
        var request = body with { Id = id };
        await _catalogItemValidator.ValidateAndThrowAsync(request, context.CancellationToken);

        if (
            !await repository.SetActiveAsync(
                kind,
                request.Id,
                request.IsActive.GetValueOrDefault(),
                context.CancellationToken
            )
        )
        {
            throw new HttpException(ApiError.NotFound);
        }

        context.Response.StatusCode = (int)HttpStatusCode.NoContent;
    }
}
