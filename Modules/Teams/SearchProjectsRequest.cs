namespace SkillIssue.Modules.Teams;

public sealed record SearchProjectsRequest(int[]? SkillIds, int? Before);
