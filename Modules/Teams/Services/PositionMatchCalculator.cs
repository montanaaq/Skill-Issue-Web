using System.Diagnostics;
using SkillIssue.Modules.Teams.Models;

namespace SkillIssue.Modules.Teams.Services;

internal static class PositionMatchCalculator
{
    public static PositionMatch Calculate(IReadOnlyList<PositionSkillMatch> skills)
    {
        if (skills.Count == 0)
        {
            return new PositionMatch(skills);
        }

        decimal weightedCoverage = 0;
        int totalImportance = 0;
        foreach (var skill in skills)
        {
            Debug.Assert(skill.RequiredLevel is >= 1 and <= 3 && skill.Importance is >= 1 and <= 3);
            decimal coverage =
                Math.Min(skill.CandidateLevel ?? 0, skill.RequiredLevel)
                / (decimal)skill.RequiredLevel;
            weightedCoverage += coverage * skill.Importance;
            totalImportance += skill.Importance;
        }

        return new PositionMatch(skills, 100 * weightedCoverage / totalImportance);
    }
}
