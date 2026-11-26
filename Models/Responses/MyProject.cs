namespace SkillIssue.Models.Responses;

public sealed record MyProject(
    int Id,
    string Name,
    string Description,
    string Role,
    string Status,
    int MemberCount,
    string[] Positions,
    List<IncomingApplication>? IncomingApplications
);

public sealed record IncomingApplication(
    int Id,
    string ApplicantName,
    string ApplicantEmail,
    string Position,
    string? CoverLetter,
    string Status,
    string? ResolutionReason,
    DateTime CreatedAt,
    bool CanAccept,
    PositionMatch Match
);

public sealed record PositionMatch(IReadOnlyList<PositionSkillMatch> Skills, decimal? Percent = null);

public sealed record PositionSkillMatch(
    string Name,
    short RequiredLevel,
    short Importance,
    short? CandidateLevel
);
