using System.Text.Json.Serialization;

namespace SkillIssue.Modules.Admin.Models;

public sealed record UpdateCatalogItemRequest(bool? IsActive)
{
    [JsonIgnore]
    public int Id { get; init; }
}
