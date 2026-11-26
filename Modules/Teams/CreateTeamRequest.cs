namespace SkillIssue.Modules.Teams;

public sealed record SuggestTeamRequest(string? Name, string? Description);

public sealed class CreateTeamRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public CreateTeamPositionRequest[]? Positions { get; init; }
}

public sealed class CreateTeamPositionRequest
{
    public string? Title { get; init; }
    public int CategoryId { get; init; }
    public int Count { get; init; }
    public bool IncludeCaptain { get; init; }
    public CreateTeamSkillRequest[]? Skills { get; init; }
}

public sealed class CreateTeamSkillRequest
{
    public int SkillId { get; init; }
    public short RequiredLevel { get; init; }
    public short Importance { get; init; }
}
