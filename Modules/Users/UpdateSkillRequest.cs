namespace SkillIssue.Modules.Users;

public sealed class UpdateSkillRequest
{
    public int SkillId { get; init; }
    public short SkillLevel { get; init; }
}
