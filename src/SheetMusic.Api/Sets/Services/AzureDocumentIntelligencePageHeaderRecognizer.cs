using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Identity;
using SheetMusic.Api.Sets.Errors;

namespace SheetMusic.Api.Sets.Services;

/// <summary>
/// Uses Azure Document Intelligence OCR to read headers from scanned score pages.
/// </summary>
public sealed class AzureDocumentIntelligencePageHeaderRecognizer(IConfiguration configuration) : IPdfPageHeaderRecognizer
{
    private const float HeaderTopFraction = 0.15F;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PdfPageHeader>> RecognizeAsync(ReadOnlyMemory<byte> pdfContent, CancellationToken cancellationToken)
    {
        var endpoint = configuration["DocumentIntelligence:Endpoint"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
            throw new OcrConfigurationError();

        var client = new DocumentIntelligenceClient(endpointUri, new DefaultAzureCredential());
        Operation<AnalyzeResult> operation;
        try
        {
            operation = await client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-read",
                BinaryData.FromBytes(pdfContent),
                cancellationToken);
        }
        catch (RequestFailedException error) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OcrUnavailableError(error);
        }

        return operation.Value.Pages.Select(CreateHeader).ToList();
    }

    private static PdfPageHeader CreateHeader(DocumentPage page)
    {
        if (page.Width is not { } width || page.Height is not { } height)
            return new PdfPageHeader(page.PageNumber, null, 0);

        var headerWords = page.Words
            .Where(word => IsInHeader(word.Polygon, width, height))
            .OrderBy(word => GetMinimumY(word.Polygon))
            .ThenBy(word => GetMinimumX(word.Polygon))
            .ToList();
        var partName = PdfPartSplitter.NormalizePartName(string.Join(' ', headerWords.Select(word => word.Content)));
        var confidence = headerWords.Count == 0 ? 0 : headerWords.Average(word => word.Confidence);
        return new PdfPageHeader(page.PageNumber, partName, confidence);
    }

    internal static bool IsInHeader(IReadOnlyList<float> polygon, float width, float height) =>
        polygon.Count >= 2 &&
        GetMinimumY(polygon) <= height * HeaderTopFraction;

    private static float GetMinimumX(IReadOnlyList<float> polygon) =>
        polygon.Where((_, index) => index % 2 == 0).Min();
    private static float GetMinimumY(IReadOnlyList<float> polygon) =>
        polygon.Where((_, index) => index % 2 != 0).Min();
}