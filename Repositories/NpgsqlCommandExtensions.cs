using System.Text.Json;
using Npgsql;
using SkillIssue.Http.Errors;

namespace SkillIssue.Repositories;

internal static class NpgsqlCommandExtensions
{
    public static async Task<T> ExecuteJsonAsync<T>(
        this NpgsqlCommand command,
        CancellationToken cancellationToken
    )
    {
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        if (
            result is not string json
            || JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web) is not T value
        )
        {
            throw new InvalidOperationException(ErrorMessages.MissingDatabaseJson);
        }

        return value;
    }

    public static async Task<T?> ExecuteNullableScalarAsync<T>(
        this NpgsqlCommand command,
        CancellationToken cancellationToken
    )
        where T : struct
    {
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : (T)result;
    }
}
