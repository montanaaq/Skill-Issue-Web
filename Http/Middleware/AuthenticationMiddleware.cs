using SkillIssue.Modules.Users;

namespace SkillIssue.Http.Middleware;

public sealed class AuthenticationMiddleware(IAuthService authService)
{
    public async Task InvokeAsync(RequestContext context, RequestDelegate next)
    {
        string? token = context.Request.Cookies[SessionCookie.Name]?.Value;

        if (!string.IsNullOrEmpty(token))
        {
            var (session, renewed) = await authService.FindSessionAsync(
                token,
                context.CancellationToken
            );
            context.User = await authService.GetCurrentUserAsync(
                session,
                context.CancellationToken
            );
            context.Session = context.User is null ? null : session;

            if (renewed && context.Session is not null)
            {
                SessionCookie.Set(context, token, context.Session.ExpiresAt);
            }
        }

        await next(context);
    }
}
