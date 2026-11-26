using SkillIssue.Modules.Users;

namespace SkillIssue.Http.Routing;

public sealed record Entry(
    string Method,
    string[] Segments,
    string Pattern,
    RequestDelegate Handler,
    IReadOnlyCollection<UserRole> AllowedRoles
);
