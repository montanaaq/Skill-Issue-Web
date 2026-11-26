using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using SkillIssue.Models.Requests;
using SkillIssue.Models.Users;

namespace SkillIssue.Repositories;

public interface IUserRepository
{
    Task<int?> CreateAsync(
        string email,
        string fullName,
        string passwordHash,
        CancellationToken cancellationToken
    );

    Task<User?> FindByEmailAsync(string email, CancellationToken ct);

    Task<User?> FindByIdAsync(int userId, CancellationToken cancellationToken);

    Task<JsonElement> GetProfileAsync(int userId, CancellationToken cancellationToken);
    Task<bool> SaveSkillsAsync(
        int userId,
        UpdateSkillRequest[] skills,
        CancellationToken cancellationToken
    );
    Task SaveSocialLinksAsync(
        int userId,
        UpdateSocialLinksRequest request,
        CancellationToken cancellationToken
    );
}

public sealed class UserRepository(NpgsqlDataSource dataSource) : IUserRepository
{
    public async Task<JsonElement> GetProfileAsync(int userId, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("SELECT public.get_user_profile($1)");
        command.Parameters.AddWithValue(userId);
        return await command.ExecuteJsonAsync<JsonElement>(cancellationToken);
    }

    public async Task<bool> SaveSkillsAsync(
        int userId,
        UpdateSkillRequest[] skills,
        CancellationToken cancellationToken
    )
    {
        await using var command = dataSource.CreateCommand(
            "CALL public.save_user_skills($1, $2, NULL)"
        );
        command.Parameters.AddWithValue(userId);
        command.Parameters.AddWithValue(
            NpgsqlDbType.Jsonb,
            JsonSerializer.Serialize(skills, JsonSerializerOptions.Web)
        );

        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task SaveSocialLinksAsync(
        int userId,
        UpdateSocialLinksRequest request,
        CancellationToken cancellationToken
    )
    {
        await using var command = dataSource.CreateCommand(
            "CALL public.save_user_social_links($1, $2)"
        );
        command.Parameters.AddWithValue(userId);
        command.Parameters.AddWithValue(
            NpgsqlDbType.Jsonb,
            JsonSerializer.Serialize(
                request.Links!.Select(link => new
                {
                    network = link.Network!.Trim(),
                    url = link.Url!.Trim(),
                })
            )
        );
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int?> CreateAsync(
        string email,
        string fullName,
        string passwordHash,
        CancellationToken cancellationToken
    )
    {
        await using var command = dataSource.CreateCommand("SELECT public.create_user($1, $2, $3)");
        command.Parameters.AddWithValue(email);
        command.Parameters.AddWithValue(fullName);
        command.Parameters.AddWithValue(passwordHash);

        return await command.ExecuteNullableScalarAsync<int>(cancellationToken);
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT * FROM public.find_user_by_email($1)"
        );
        command.Parameters.AddWithValue(email);

        await using var reader = await command.ExecuteReaderAsync(ct);

        return await ReadUserAsync(reader, ct);
    }

    public async Task<User?> FindByIdAsync(int userId, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT * FROM public.find_user_by_id($1)"
        );
        command.Parameters.AddWithValue(userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await ReadUserAsync(reader, cancellationToken);
    }

    private static async Task<User?> ReadUserAsync(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken
    )
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new User
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            FullName = reader.GetString(reader.GetOrdinal("full_name")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            Role = Enum.Parse<UserRole>(reader.GetString(reader.GetOrdinal("role")), true),
            ShowInRecommendations = reader.GetBoolean(reader.GetOrdinal("show_in_recommendations")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
        };
    }
}
