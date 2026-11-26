namespace SkillIssue.Modules.Teams.Models;

public sealed class TeamPosition
{
    public int Id { get; init; }
    public int TeamId { get; init; }
    public int CategoryId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public bool IsOpen { get; init; } = true;
    public DateTime? DeletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
