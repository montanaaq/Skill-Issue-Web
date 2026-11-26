namespace SkillIssue.Modules.Teams;

public sealed class CreateApplicationRequest
{
    internal int TeamId { get; set; }
    public int PositionId { get; init; }
    public string? CoverLetter { get; init; }
}
