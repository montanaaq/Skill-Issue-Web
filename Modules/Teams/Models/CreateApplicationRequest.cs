namespace SkillIssue.Modules.Teams.Models;

public sealed class CreateApplicationRequest
{
    internal int TeamId { get; set; }
    public int PositionId { get; init; }
    public string? CoverLetter { get; init; }
}
