using SkillIssue.Controllers;
using SkillIssue.Models.Users;
using SkillIssue.Repositories;
using SkillIssue.Services;

namespace SkillIssue.Http.Routing;

internal static class RouteMappings
{
    private static readonly UserRole[] _publicRoles =
    [
        UserRole.Guest,
        UserRole.User,
        UserRole.Admin,
    ];

    public static void MapPages(this Router router)
    {
        router.Map("GET", "/", PagesController.HomeAsync, _publicRoles);
        router.Map("GET", "/projects", PagesController.ProjectsAsync, _publicRoles);
        router.Map("GET", "/signin", PagesController.SignInAsync, _publicRoles);
        router.Map("GET", "/signup", PagesController.SignUpAsync, _publicRoles);

        router.Map("GET", "/profile", PagesController.ProfileAsync);
        router.Map("GET", "/my-projects", PagesController.MyProjectsAsync);
    }

    public static void MapUserApi(
        this Router router,
        IAuthService authService,
        IUserRepository repository
    )
    {
        var users = new UserController(authService, repository);

        router.Map("POST", "/api/user/signin", users.SignInAsync, _publicRoles);
        router.Map("POST", "/api/user/signup", users.SignUpAsync, _publicRoles);
        router.Map("POST", "/api/user/signout", users.SignOutAsync, _publicRoles);
        router.Map("GET", "/api/user/me", users.MeAsync);
        router.Map("GET", "/api/user/profile", users.ProfileAsync);
        router.Map("POST", "/api/user/skills", users.SaveSkillsAsync);
        router.Map("POST", "/api/user/social-links", users.SaveSocialLinksAsync);
    }

    public static void MapTeamsApi(
        this Router router,
        ITeamRepository repository,
        IUserRepository userRepository,
        ProjectAiService aiService
    )
    {
        var teams = new TeamController(repository, userRepository, aiService);

        router.Map("GET", "/api/teams/catalog", teams.CatalogAsync, _publicRoles);
        router.Map("GET", "/api/teams/mine", teams.MyProjectsAsync);
        router.Map("POST", "/api/teams/search", teams.SearchProjectsAsync, _publicRoles);
        router.Map("POST", "/api/teams/suggest", teams.SuggestAsync);
        router.Map("GET", "/api/teams/recommendations", teams.RecommendationsAsync);
        router.Map(
            "POST",
            "/api/teams/recommendations/{positionId}/explanation",
            teams.ExplainRecommendationAsync
        );
        router.Map("POST", "/api/teams", teams.CreateAsync);
        router.Map("POST", "/api/teams/{teamId}/applications", teams.ApplyAsync);
        router.Map(
            "POST",
            "/api/teams/{teamId}/applications/{applicationId}",
            teams.ResolveApplicationAsync
        );
    }
}
