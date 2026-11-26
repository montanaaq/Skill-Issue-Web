namespace SkillIssue.Modules.Users;

public static class LinkValidation
{
    public static bool IsValidLink(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Length <= 500
            && Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.Host.Length > 0
            && uri.IsDefaultPort
            && uri.UserInfo.Length == 0;
    }
}
