namespace SkillIssue.Modules.Teams.Models;

public sealed record SearchProjectsRequest(int[]? SkillIds, int? Before);
