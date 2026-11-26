using System.Text.Json.Serialization;

namespace SkillIssue.Modules.Users.Models;

public sealed class User
{
    public int Id { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }

    [JsonIgnore]
    public string PasswordHash { get; init; } = string.Empty;
    public UserRole Role { get; init; } = UserRole.User;
    public bool ShowInRecommendations { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
