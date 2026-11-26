namespace SkillIssue.Modules.Teams.Models;

public sealed record TeamCatalog(List<CatalogSkill> Skills, List<CatalogCategory> Categories);

public sealed record CatalogSkill(int Id, string Name, int CategoryId, int[] RelatedSkillIds);

public sealed record CatalogCategory(int Id, string Name);
