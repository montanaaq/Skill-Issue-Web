namespace SkillIssue.Modules.Teams.Models;

public sealed class Application
{
    public int Id { get; init; }
    public int PositionId { get; init; }
    public int ApplicantId { get; init; }
    public string? CoverLetter { get; init; }
    public ApplicationStatus Status { get; init; } = ApplicationStatus.Pending;
    public DateTime CreatedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? ResolutionReason { get; init; }
}
