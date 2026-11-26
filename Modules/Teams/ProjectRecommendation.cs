namespace SkillIssue.Modules.Teams;

public sealed record ProjectRecommendation(
    int TeamId,
    string TeamName,
    string Description,
    int PositionId,
    string PositionTitle,
    string PositionCategory,
    decimal Percent,
    PositionSkillMatch[] Skills
);
