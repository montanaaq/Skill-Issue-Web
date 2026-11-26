using Npgsql;
using SkillIssue.Modules.Admin.Models;

namespace SkillIssue.Modules.Admin.Repositories;

public sealed class AdminCatalogRepository(NpgsqlDataSource dataSource)
{
    public async Task<AdminCatalogItem[]> GetAsync(CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("SELECT public.get_admin_catalog()");
        return await command.ExecuteJsonAsync<AdminCatalogItem[]>(cancellationToken);
    }

    public async Task<bool> SetActiveAsync(
        string kind,
        int id,
        bool isActive,
        CancellationToken cancellationToken
    )
    {
        await using var command = dataSource.CreateCommand(
            "CALL public.set_catalog_item_active($1, $2, $3, NULL)"
        );
        command.Parameters.AddWithValue(kind);
        command.Parameters.AddWithValue(id);
        command.Parameters.AddWithValue(isActive);
        return await command.ExecuteNullableScalarAsync<bool>(cancellationToken) == true;
    }
}
