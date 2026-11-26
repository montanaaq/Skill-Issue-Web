namespace SkillIssue.Modules.Users.Models;

public sealed class SignUpRequest
{
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? FullName { get; init; }
}
