namespace SkillIssue.Modules.Teams.Models;

public sealed class Skill
{
    public int Id { get; init; }
    public int CategoryId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}
