using System.Text.Json.Serialization;

namespace SkillIssue.Modules.Users.Models;

public sealed record SignInResult(
    User User,
    [property: JsonIgnore] string Token,
    DateTime ExpiresAt
);
