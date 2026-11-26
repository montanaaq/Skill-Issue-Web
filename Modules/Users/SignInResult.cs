using System.Text.Json.Serialization;

namespace SkillIssue.Modules.Users;

public sealed record SignInResult(
    User User,
    [property: JsonIgnore] string Token,
    DateTime ExpiresAt
);
