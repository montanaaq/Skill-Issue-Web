namespace SkillIssue.Modules.Teams.Models;

public sealed record TeamSuggestion(
    string? Question,
    string? Language,
    CreateTeamPositionRequest[]? Positions
);
