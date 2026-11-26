using Npgsql;
using SkillIssue.Modules.Teams.Models;

namespace SkillIssue.Configuration;

public static class DatabaseSettings
{
    public static NpgsqlDataSource CreateDataSource(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);

        builder.MapEnum<ClosureKind>("closure_kind");
        builder.MapEnum<ApplicationStatus>("application_status");
        builder.MapEnum<InvitationStatus>("invitation_status");

        return builder.Build();
    }
}
