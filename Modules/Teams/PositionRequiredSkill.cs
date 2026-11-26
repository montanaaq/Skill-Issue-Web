namespace SkillIssue.Modules.Teams;

public sealed class PositionRequiredSkill
{
    public int PositionId { get; init; }
    public int SkillId { get; init; }
    public short RequiredLevel { get; init; }
    public short Importance { get; init; } = 2;
}
