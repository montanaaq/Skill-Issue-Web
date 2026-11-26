using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace SkillIssue.Modules.Teams;

public interface ITeamRepository
{
    Task<List<MyProject>> GetMyProjectsAsync(int userId, CancellationToken cancellationToken);
    Task<TeamCatalog> GetCatalogAsync(CancellationToken cancellationToken);
    Task<ProjectRecommendation[]> GetRecommendationsAsync(
        int userId,
        int? positionId,
        int limit,
        CancellationToken cancellationToken
    );
    Task<List<TeamSearchResult>> SearchAsync(
        int[] skillIds,
        int beforeId,
        int? viewerId,
        CancellationToken cancellationToken,
        bool matchAnySkill = false
    );
    Task<int?> CreateAsync(
        int captainId,
        CreateTeamRequest request,
        CancellationToken cancellationToken
    );
    Task<int?> ApplyAsync(
        int teamId,
        int positionId,
        int applicantId,
        string? coverLetter,
        CancellationToken cancellationToken
    );
    Task<bool?> ResolveApplicationAsync(
        int teamId,
        int applicationId,
        int captainId,
        bool accept,
        CancellationToken cancellationToken
    );
}

public sealed class TeamRepository(NpgsqlDataSource dataSource) : ITeamRepository
{
    public async Task<List<MyProject>> GetMyProjectsAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        await using var command = dataSource.CreateCommand("SELECT public.get_my_projects($1)");
        command.Parameters.AddWithValue(userId);
        var projects = await command.ExecuteJsonAsync<List<MyProject>>(cancellationToken);

        foreach (var project in projects)
        {
            if (project.IncomingApplications is not { } applications)
            {
                continue;
            }

            for (int index = 0; index < applications.Count; index++)
            {
                var application = applications[index];
                applications[index] = application with
                {
                    Match = PositionMatchCalculator.Calculate(application.Match.Skills),
                };
            }
        }

        return projects;
    }

    public async Task<TeamCatalog> GetCatalogAsync(CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT * FROM public.get_team_catalog()"
        );
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var skills = new List<CatalogSkill>();
        var categories = new List<CatalogCategory>();

        while (await reader.ReadAsync(cancellationToken))
        {
            int id = reader.GetInt32(1);
            string name = reader.GetString(2);
            if (reader.GetString(0) == "skill")
            {
                skills.Add(
                    new CatalogSkill(id, name, reader.GetInt32(3), reader.GetFieldValue<int[]>(4))
                );
            }
            else
            {
                categories.Add(new CatalogCategory(id, name));
            }
        }

        return new TeamCatalog(skills, categories);
    }

    public async Task<List<TeamSearchResult>> SearchAsync(
        int[] skillIds,
        int beforeId,
        int? viewerId,
        CancellationToken cancellationToken,
        bool matchAnySkill = false
    )
    {
        await using var command = dataSource.CreateCommand(
            "SELECT * FROM public.search_teams($1, $2, $3, $4)"
        );
        command.Parameters.AddWithValue(skillIds);
        command.Parameters.AddWithValue(beforeId);
        command.Parameters.AddWithValue(NpgsqlDbType.Integer, (object?)viewerId ?? DBNull.Value);
        command.Parameters.AddWithValue(matchAnySkill);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var teams = new List<TeamSearchResult>();

        while (await reader.ReadAsync(cancellationToken))
        {
            teams.Add(
                new TeamSearchResult(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetFieldValue<string[]>(4),
                    reader.GetFieldValue<string[]>(5),
                    reader.GetFieldValue<int[]>(6),
                    reader.GetBoolean(7),
                    JsonSerializer.Deserialize<JsonElement>(reader.GetString(8)),
                    reader.GetBoolean(9)
                )
            );
        }

        return teams;
    }

    public async Task<int?> CreateAsync(
        int captainId,
        CreateTeamRequest request,
        CancellationToken cancellationToken
    )
    {
        await using var command = dataSource.CreateCommand(
            "SELECT public.create_team($1, $2, $3, $4)"
        );
        command.Parameters.AddWithValue(captainId);
        command.Parameters.AddWithValue(request.Name!.Trim());
        command.Parameters.AddWithValue(request.Description!.Trim());
        command.Parameters.AddWithValue(
            NpgsqlDbType.Jsonb,
            JsonSerializer.Serialize(request.Positions!, JsonSerializerOptions.Web)
        );

        return await command.ExecuteNullableScalarAsync<int>(cancellationToken);
    }

    public async Task<ProjectRecommendation[]> GetRecommendationsAsync(
        int userId,
        int? positionId,
        int limit,
        CancellationToken cancellationToken
    )
    {
        await using var command = dataSource.CreateCommand(
            "SELECT public.get_project_recommendations($1, $2, $3)"
        );
        command.Parameters.AddWithValue(userId);
        command.Parameters.AddWithValue(NpgsqlDbType.Integer, (object?)positionId ?? DBNull.Value);
        command.Parameters.AddWithValue(limit);
        return await command.ExecuteJsonAsync<ProjectRecommendation[]>(cancellationToken);
    }

    public async Task<int?> ApplyAsync(
        int teamId,
        int positionId,
        int applicantId,
        string? coverLetter,
        CancellationToken cancellationToken
    )
    {
        await using var command = dataSource.CreateCommand(
            "CALL public.apply_to_team_position($1, $2, $3, $4, NULL)"
        );
        command.Parameters.AddWithValue(teamId);
        command.Parameters.AddWithValue(positionId);
        command.Parameters.AddWithValue(applicantId);
        command.Parameters.AddWithValue(NpgsqlDbType.Text, (object?)coverLetter ?? DBNull.Value);

        return await command.ExecuteNullableScalarAsync<int>(cancellationToken);
    }

    public async Task<bool?> ResolveApplicationAsync(
        int teamId,
        int applicationId,
        int captainId,
        bool accept,
        CancellationToken cancellationToken
    )
    {
        await using var command = dataSource.CreateCommand(
            "CALL public.resolve_team_application($1, $2, $3, $4, NULL)"
        );
        command.Parameters.AddWithValue(teamId);
        command.Parameters.AddWithValue(applicationId);
        command.Parameters.AddWithValue(captainId);
        command.Parameters.AddWithValue(accept);

        return await command.ExecuteNullableScalarAsync<bool>(cancellationToken);
    }
}
