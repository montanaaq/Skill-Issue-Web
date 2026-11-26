using System.Net;
using System.Text.Json;
using FluentValidation;
using SkillIssue.Http;
using SkillIssue.Http.Errors;
using SkillIssue.Models.Requests;
using SkillIssue.Repositories;
using SkillIssue.Templates;
using SkillIssue.Validators;

namespace SkillIssue.Controllers;

public sealed class TeamController(ITeamRepository repository)
{
    private const int PageSize = 12;
    private readonly CreateTeamRequestValidator _validator = new();
    private readonly CreateApplicationRequestValidator _applicationValidator = new();

    public async Task MyProjectsAsync(RequestContext context)
    {
        var user = context.User ?? throw new HttpException(ApiError.Unauthorized);
        var projects = await repository.GetMyProjectsAsync(user.Id, context.CancellationToken);
        await context.WriteJsonAsync(JsonSerializer.SerializeToElement(projects, JsonSerializerOptions.Web));
    }

    public async Task CatalogAsync(RequestContext context)
    {
        var catalog = await repository.GetCatalogAsync(context.CancellationToken);
        await context.WriteJsonAsync(
            new
            {
                skills = catalog.Skills.Select(skill => new
                {
                    id = skill.Id,
                    name = skill.Name,
                    categoryId = skill.CategoryId,
                    relatedSkillIds = skill.RelatedSkillIds,
                }),
                categories = catalog.Categories.Select(category => new
                {
                    id = category.Id,
                    name = category.Name,
                }),
            }
        );
    }

    public async Task SearchProjectsAsync(RequestContext context)
    {
        var request = await context.ReadJsonAsync<SearchProjectsRequest>();
        if (
            request.SkillIds is not { } skillIds
            || skillIds.Any(id => id <= 0)
            || request.Before is <= 0
        )
        {
            throw new HttpException(ApiError.BadRequest);
        }

        var teams = await repository.SearchAsync(
            skillIds.Distinct().ToArray(),
            request.Before ?? int.MaxValue,
            context.User?.Id,
            context.CancellationToken,
            matchAnySkill: true
        );
        string html = await TemplateEngine.RenderAsync(
            "components/ProjectResults.html",
            new ProjectResultsData(
                IsSignedIn: context.User is not null,
                HasProjects: teams.Count > 0,
                Projects: teams.Take(PageSize).ToArray()
            ),
            context.CancellationToken
        );
        int? nextBefore = teams.Count > PageSize ? teams[PageSize - 1].Id : null;
        await context.WriteJsonAsync(new { html, nextBefore });
    }

    public async Task CreateAsync(RequestContext context)
    {
        var user = context.User ?? throw new HttpException(ApiError.Unauthorized);
        var request = await context.ReadJsonAsync<CreateTeamRequest>();

        await _validator.ValidateAndThrowAsync(request, context.CancellationToken);
        int teamId =
            await repository.CreateAsync(user.Id, request, context.CancellationToken)
            ?? throw new HttpException(ApiError.BadRequest);

        await context.WriteJsonAsync(new { teamId }, HttpStatusCode.Created);
    }

    public async Task ApplyAsync(RequestContext context)
    {
        var user = context.User ?? throw new HttpException(ApiError.Unauthorized);
        var request = await context.ReadJsonAsync<CreateApplicationRequest>();
        _ = int.TryParse(context.RouteValues["teamId"], out int teamId);
        request.TeamId = teamId;

        await _applicationValidator.ValidateAndThrowAsync(request, context.CancellationToken);

        int applicationId =
            await repository.ApplyAsync(
                request.TeamId,
                request.PositionId,
                user.Id,
                request.CoverLetter?.Trim(),
                context.CancellationToken
            ) ?? throw new HttpException(ApiError.Conflict, ErrorMessages.ApplicationUnavailable);

        await context.WriteJsonAsync(new { applicationId }, HttpStatusCode.Created);
    }

    public async Task ResolveApplicationAsync(RequestContext context)
    {
        var user = context.User ?? throw new HttpException(ApiError.Unauthorized);
        if (
            !int.TryParse(context.RouteValues["teamId"], out int teamId)
            || teamId <= 0
            || !int.TryParse(context.RouteValues["applicationId"], out int applicationId)
            || applicationId <= 0
        )
        {
            throw new HttpException(ApiError.BadRequest);
        }

        var request = await context.ReadJsonAsync<ResolveApplicationRequest>();
        bool accept = request.Decision switch
        {
            "accept" => true,
            "reject" => false,
            _ => throw new HttpException(ApiError.BadRequest),
        };
        bool resolved =
            await repository.ResolveApplicationAsync(
                teamId,
                applicationId,
                user.Id,
                accept,
                context.CancellationToken
            ) ?? throw new HttpException(ApiError.NotFound);
        if (!resolved)
        {
            throw new HttpException(ApiError.Conflict, ErrorMessages.ApplicationAlreadyResolved);
        }

        await context.WriteJsonAsync(new { applicationId });
    }
}
