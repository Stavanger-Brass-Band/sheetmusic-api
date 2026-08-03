namespace SheetMusic.Agents;

/// <summary>
/// Input for closed-set music-part matching.
/// </summary>
public sealed record PartClassificationRequest(string FileName, IReadOnlyList<string> CandidateNames);

/// <summary>
/// Input for closed-set category classification.
/// </summary>
public sealed record CategoryClassificationRequest(
    string? Title,
    string? Composer,
    string? Arranger,
    IReadOnlyList<string> ProjectNames,
    IReadOnlyList<string> CandidateCategories,
    IReadOnlyList<CategoryExample> Examples);

/// <summary>
/// A human-labelled example used to ground category classification in the band's terminology.
/// </summary>
public sealed record CategoryExample(string Text, IReadOnlyList<string> Categories);

/// <summary>
/// A part classification result. A null match means the model abstained.
/// </summary>
public sealed record PartClassificationResult(string? Match);

/// <summary>
/// A category classification result. An empty list means the model abstained.
/// </summary>
public sealed record CategoryClassificationResult(IReadOnlyList<string> Categories);
