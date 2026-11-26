using System.Text.Json;

namespace SkillIssue.Modules.Teams.Models;

public sealed record TeamSearchResult(
    int Id,
    string Name,
    string Description,
    int MemberCount,
    string[] Positions,
    string[] RequiredSkills,
    int[] PositionIds,
    bool IsMember,
    JsonElement ApplicationPositions,
    bool HasPendingApplication
);
