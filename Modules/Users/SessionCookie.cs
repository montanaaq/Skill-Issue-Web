using SkillIssue.Http;

namespace SkillIssue.Modules.Users;

public static class SessionCookie
{
    public const string Name = "auth_session";

    public static void Delete(RequestContext context)
    {
        Set(context, string.Empty, DateTime.UnixEpoch);
    }

    public static void Set(RequestContext context, string token, DateTime expiresAt)
    {
        context.Response.Headers["Set-Cookie"] =
            $"{Name}={token}; Path=/; HttpOnly; SameSite=Lax; Expires={expiresAt:R}"
            + (context.Request.IsSecureConnection ? "; Secure" : string.Empty);
    }
}
