namespace SheetMusic.Api.Sets.Services;

public interface IMetadataAgentClient
{
    Task<string?> ClassifyPartAsync(string fileName, IReadOnlyList<string> candidateNames, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ClassifyCategoryAsync(
        string? title,
        string? composer,
        string? arranger,
        IReadOnlyList<string> projectNames,
        IReadOnlyList<string> candidateCategories,
        IReadOnlyList<CategoryExample> examples,
        CancellationToken cancellationToken);
}

public sealed record CategoryExample(string Text, IReadOnlyList<string> Categories);
