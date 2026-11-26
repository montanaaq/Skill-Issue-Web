namespace SkillIssue.Modules.Users.Models;

public sealed class UserSocialLink
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public required string Network { get; init; }
    public required string Url { get; init; }
}
