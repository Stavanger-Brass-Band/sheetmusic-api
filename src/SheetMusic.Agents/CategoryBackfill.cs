using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SheetMusic.Agents;

public sealed class CategoryBackfill(AgentDbContext db, MetadataAgent metadataAgent, ILogger<CategoryBackfill> logger)
{
    public const string PromptVersion = "category-v1";
    public const string ModelVersion = "gpt-5-mini";

    public async Task RunAsync(int? limit, bool dryRun, CancellationToken cancellationToken)
    {
        var candidates = await db.Categories
            .Where(category => !category.Inactive)
            .OrderBy(category => category.Name)
            .Select(category => category.Name)
            .ToListAsync(cancellationToken);

        var examples = await (
            from set in db.Sets
            join assignment in db.SetCategories on set.Id equals assignment.SheetMusicSetId
            join category in db.Categories on assignment.CategoryId equals category.Id
            where assignment.Source == "Human" && !category.Inactive
            orderby set.Id
            select new CategoryExample(set.Title, new[] { category.Name }))
            .Take(20)
            .ToListAsync(cancellationToken);

        IQueryable<AgentSet> sets = db.Sets
            .Where(set => !db.SetCategories.Any(assignment => assignment.SheetMusicSetId == set.Id))
            .OrderBy(set => set.Id);
        if (limit is not null)
            sets = sets.Take(limit.Value);

        var projectNames = await (
            from projectSet in db.ProjectSets
            join project in db.Projects on projectSet.ProjectId equals project.Id
            select new { projectSet.SheetMusicSetId, project.Name })
            .ToListAsync(cancellationToken);
        var namesBySet = projectNames
            .GroupBy(item => item.SheetMusicSetId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.Select(item => item.Name).ToArray());

        foreach (var set in await sets.ToListAsync(cancellationToken))
        {
            var result = await metadataAgent.ClassifyCategoryAsync(
                new CategoryClassificationRequest(
                    set.Title,
                    set.Composer,
                    set.Arranger,
                    namesBySet.GetValueOrDefault(set.Id, []),
                    candidates,
                    examples),
                cancellationToken);

            if (result.Categories.Count == 0)
            {
                logger.LogInformation("No category suggestion for set {SetId} ({Title})", set.Id, set.Title);
                continue;
            }

            logger.LogInformation("Suggested categories for set {SetId} ({Title}): {Categories}", set.Id, set.Title, string.Join(", ", result.Categories));
            if (dryRun)
                continue;

            var categoryIds = await db.Categories
                .Where(category => result.Categories.Contains(category.Name) && !category.Inactive)
                .ToDictionaryAsync(category => category.Name, category => category.Id, cancellationToken);
            foreach (var categoryName in result.Categories)
            {
                if (!categoryIds.TryGetValue(categoryName, out var categoryId))
                    continue;

                db.SetCategories.Add(new AgentSheetMusicCategory
                {
                    Id = Guid.NewGuid(),
                    SheetMusicSetId = set.Id,
                    CategoryId = categoryId,
                    Source = "Ai",
                    ModelVersion = ModelVersion,
                    PromptVersion = PromptVersion,
                    SuggestedAt = DateTimeOffset.UtcNow,
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
