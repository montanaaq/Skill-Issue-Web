namespace SkillIssue.Models.Requests;

public sealed class UpdateSkillRequest
{
    public int SkillId { get; init; }
    public short SkillLevel { get; init; }
}
