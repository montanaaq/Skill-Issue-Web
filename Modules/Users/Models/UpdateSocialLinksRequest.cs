namespace SkillIssue.Modules.Users.Models;

public sealed class UpdateSocialLinksRequest
{
    public SocialLinkRequest[]? Links { get; init; }
}

public sealed class SocialLinkRequest
{
    public string? Network { get; init; }
    public string? Url { get; init; }
}
