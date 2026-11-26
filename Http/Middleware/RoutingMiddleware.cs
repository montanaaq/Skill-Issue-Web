using SkillIssue.Http.Errors;
using SkillIssue.Http.Routing;
using SkillIssue.Modules.Users;

namespace SkillIssue.Http.Middleware;

public sealed class RoutingMiddleware(Router router)
{
    public async Task InvokeAsync(RequestContext context, RequestDelegate next)
    {
        var match = router.Match(context.Request.HttpMethod, context.Path);
        if (match is null)
        {
            await next(context);
            return;
        }

        if (match.Handler is null)
        {
            context.Response.Headers["Allow"] = string.Join(", ", match.AllowedMethods);
            throw new HttpException(ApiError.MethodNotAllowed);
        }

        if (!match.AllowedRoles.Contains(context.Role))
        {
            throw new HttpException(
                context.Role == UserRole.Guest ? ApiError.Unauthorized : ApiError.Forbidden
            );
        }

        context.RouteValues = match.Values;
        await match.Handler(context);
    }
}
