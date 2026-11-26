using System.Text.Json.Serialization;

namespace SkillIssue.Modules.Users.Models;

public sealed class Session
{
    public int Id { get; init; }
    public int UserId { get; init; }

    [JsonIgnore]
    public byte[] TokenHash { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? RevokedAt { get; init; }
}
