namespace SkillIssue.Modules.Teams.Models;

public sealed class TeamEvent
{
    public int Id { get; init; }
    public int TeamId { get; init; }
    public required TeamEventType EventType { get; init; }
    public int ActorUserId { get; init; }
    public int? SubjectUserId { get; init; }
    public DateTime OccurredAt { get; init; }
}
