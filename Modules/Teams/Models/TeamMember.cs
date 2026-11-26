namespace SkillIssue.Modules.Teams.Models;

public sealed class TeamMember
{
    public int Id { get; init; }
    public int TeamId { get; init; }
    public int UserId { get; init; }
    public DateTime JoinedAt { get; init; }
    public DateTime? LeftAt { get; init; }
}
