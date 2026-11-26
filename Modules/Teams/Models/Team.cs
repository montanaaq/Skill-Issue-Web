namespace SkillIssue.Modules.Teams.Models;

public sealed class Team
{
    public int Id { get; init; }
    public int CaptainId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public DateTime? ClosedAt { get; init; }
    public ClosureKind? ClosureKind { get; init; }
    public DateTime? DeletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
