namespace SkillIssue.Modules.Teams.Models;

public sealed class Invitation
{
    public int Id { get; init; }
    public int PositionId { get; init; }
    public int InviteeId { get; init; }
    public string? Message { get; init; }
    public InvitationStatus Status { get; init; } = InvitationStatus.Pending;
    public DateTime CreatedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}
