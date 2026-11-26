using Npgsql;

namespace SkillIssue.Modules.Users;

public interface ISessionRepository
{
    Task<int> CreateAsync(
        int userId,
        byte[] tokenHash,
        DateTime expiresAt,
        CancellationToken cancellationToken
    );

    Task<Session?> FindByTokenHashAsync(byte[] tokenHash, CancellationToken cancellationToken);

    Task<Session?> RenewAsync(
        int sessionId,
        DateTime expiresAt,
        CancellationToken cancellationToken
    );

    Task RevokeAsync(byte[] tokenHash, CancellationToken cancellationToken);
}

public sealed class SessionRepository(NpgsqlDataSource dataSource) : ISessionRepository
{
    public async Task<int> CreateAsync(
        int userId,
        byte[] tokenHash,
        DateTime expiresAt,
        CancellationToken cancellationToken
    )
    {
        await using var command = dataSource.CreateCommand(
            "SELECT public.create_session($1, $2, $3)"
        );

        command.Parameters.AddWithValue(userId);
        command.Parameters.AddWithValue(tokenHash);
        command.Parameters.AddWithValue(expiresAt);

        return (int)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task<Session?> FindByTokenHashAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken
    )
    {
        await using var command = dataSource.CreateCommand(
            "SELECT * FROM public.find_session_by_token_hash($1)"
        );

        command.Parameters.AddWithValue(tokenHash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await ReadSessionAsync(reader, cancellationToken);
    }

    public async Task<Session?> RenewAsync(
        int sessionId,
        DateTime expiresAt,
        CancellationToken cancellationToken
    )
    {
        await using var command = dataSource.CreateCommand(
            "CALL public.renew_session($1, $2, NULL, NULL, NULL, NULL, NULL, NULL)"
        );

        command.Parameters.AddWithValue(sessionId);
        command.Parameters.AddWithValue(expiresAt);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await ReadSessionAsync(reader, cancellationToken);
    }

    public async Task RevokeAsync(byte[] tokenHash, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("CALL public.revoke_session($1)");
        command.Parameters.AddWithValue(tokenHash);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Session?> ReadSessionAsync(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken
    )
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        int idOrdinal = reader.GetOrdinal("id");
        if (reader.IsDBNull(idOrdinal))
        {
            return null;
        }

        int revokedAtOrdinal = reader.GetOrdinal("revoked_at");

        return new Session
        {
            Id = reader.GetInt32(idOrdinal),
            UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
            TokenHash = reader.GetFieldValue<byte[]>(reader.GetOrdinal("token_hash")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            ExpiresAt = reader.GetDateTime(reader.GetOrdinal("expires_at")),
            RevokedAt = reader.IsDBNull(revokedAtOrdinal)
                ? null
                : reader.GetDateTime(revokedAtOrdinal),
        };
    }
}
