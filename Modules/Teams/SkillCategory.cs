namespace SkillIssue.Modules.Teams;

public sealed class SkillCategory
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public bool IsActive { get; init; } = true;
}
