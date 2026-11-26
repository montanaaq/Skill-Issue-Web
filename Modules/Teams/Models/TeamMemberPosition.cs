namespace SkillIssue.Modules.Teams.Models;

public sealed class TeamMemberPosition
{
    public int Id { get; init; }
    public int MembershipId { get; init; }
    public int PositionId { get; init; }
    public required string PositionTitleAtAssignment { get; init; }
    public DateTime AssignedAt { get; init; }
    public DateTime? EndedAt { get; init; }
}
