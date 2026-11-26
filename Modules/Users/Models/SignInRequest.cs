namespace SkillIssue.Modules.Users.Models;

public sealed class SignInRequest
{
    public string? Email { get; init; }
    public string? Password { get; init; }
}
