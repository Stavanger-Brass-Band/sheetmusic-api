using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SheetMusic.Api.Sets.Errors;

namespace SheetMusic.Api.Sets.Services;

/// <summary>
/// Splits a multi-part score into PDFs using OCR-recognized page headers.
/// </summary>
public sealed class PdfPartSplitter(IPdfPageHeaderRecognizer pageHeaderRecognizer, IPdfPartNameExtractor partNameExtractor)
{
    /// <summary>
    /// Splits <paramref name="source"/> into locally generated PDFs and returns the detected page groups.
    /// </summary>
    public async Task<PdfPartSplitResult> SplitAsync(Stream source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        await using var input = new MemoryStream();
        await source.CopyToAsync(input, cancellationToken);
        var content = input.ToArray();
        var pageCount = GetPageCount(content);
        var recognizedHeaders = await pageHeaderRecognizer.RecognizeAsync(content, cancellationToken);
        var sourceHeaders = GetHeadersForEveryPage(recognizedHeaders, pageCount);
        var headers = await ExtractPartNamesAsync(sourceHeaders, cancellationToken);
        var result = GroupHeaders(headers) with { SourceHeaders = sourceHeaders };

        foreach (var group in result.Groups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            group.Content = ExtractPages(content, group.StartPage, group.EndPage);
        }

        return result;
    }

    private async Task<IReadOnlyList<PdfPageHeader>> ExtractPartNamesAsync(IReadOnlyList<PdfPageHeader> headers, CancellationToken cancellationToken)
    {
        var distinctHeaders = headers
            .Where(header => !string.IsNullOrWhiteSpace(header.NormalizedPartName))
            .Select(header => header.NormalizedPartName!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var extractedNames = await partNameExtractor.ExtractPartNamesAsync(distinctHeaders, cancellationToken);
        if (extractedNames.Count != distinctHeaders.Count)
            extractedNames = [.. distinctHeaders.Select(_ => (string?)null)];
        var extractedNamesByHeader = distinctHeaders
            .Zip(extractedNames, (header, partName) => new { header, partName })
            .ToDictionary(item => item.header, item => item.partName, StringComparer.Ordinal);

        return headers.Select(header => string.IsNullOrWhiteSpace(header.NormalizedPartName)
            ? header
            : header with { NormalizedPartName = extractedNamesByHeader.GetValueOrDefault(header.NormalizedPartName) })
            .ToList();
    }

    private static int GetPageCount(byte[] content)
    {
        try
        {
            using var document = PdfReader.Open(new MemoryStream(content), PdfDocumentOpenMode.Import);
            return document.PageCount;
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            throw new InvalidPartsPdfError(error);
        }
    }

    private static IReadOnlyList<PdfPageHeader> GetHeadersForEveryPage(IReadOnlyList<PdfPageHeader> recognizedHeaders, int pageCount)
    {
        var headersByPage = recognizedHeaders
            .Where(header => header.PageNumber is > 0 && header.PageNumber <= pageCount)
            .GroupBy(header => header.PageNumber)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(header => header.Confidence).First());

        return Enumerable.Range(1, pageCount)
            .Select(pageNumber => headersByPage.GetValueOrDefault(pageNumber, new PdfPageHeader(pageNumber, null, 0)))
            .ToList();
    }

    internal static PdfPartSplitResult GroupHeaders(IEnumerable<PdfPageHeader> headers)
    {
        var groups = new List<PdfPartGroup>();
        var diagnostics = new List<PdfPartSplitDiagnostic>();
        PdfPartGroup? currentGroup = null;

        foreach (var header in headers.OrderBy(header => header.PageNumber))
        {
            var normalizedPartName = NormalizePartName(header.NormalizedPartName);
            if (string.IsNullOrWhiteSpace(normalizedPartName))
            {
                if (currentGroup is not null && currentGroup.NormalizedPartName != "UNRECOGNIZED")
                {
                    diagnostics.Add(new PdfPartSplitDiagnostic(header.PageNumber, currentGroup.NormalizedPartName, header.Confidence, "PartNameInherited", "The part name could not be read; the page was assigned to the previous part."));
                    currentGroup.EndPage = header.PageNumber;
                    currentGroup.Confidence = Math.Min(currentGroup.Confidence, header.Confidence);
                    continue;
                }

                diagnostics.Add(new PdfPartSplitDiagnostic(header.PageNumber, null, 0, "HeaderUnreadable", "The header could not be read; the page was placed in an unrecognized group for review."));

                if (currentGroup?.NormalizedPartName == "UNRECOGNIZED")
                {
                    currentGroup.EndPage = header.PageNumber;
                }
                else
                {
                    currentGroup = new PdfPartGroup("UNRECOGNIZED", header.PageNumber, header.PageNumber, 0, "HeaderUnreadable");
                    groups.Add(currentGroup);
                }
                continue;
            }

            if (currentGroup is null || !string.Equals(currentGroup.NormalizedPartName, normalizedPartName, StringComparison.Ordinal))
            {
                currentGroup = new PdfPartGroup(normalizedPartName, header.PageNumber, header.PageNumber, header.Confidence, "OcrHeaderText");
                groups.Add(currentGroup);
            }
            else
            {
                currentGroup.EndPage = header.PageNumber;
                currentGroup.Confidence = Math.Min(currentGroup.Confidence, header.Confidence);
            }

            if (header.Confidence < 1)
                diagnostics.Add(new PdfPartSplitDiagnostic(header.PageNumber, normalizedPartName, header.Confidence, "HeaderAmbiguous", "The detected header should be reviewed."));
        }

        return new PdfPartSplitResult(groups, diagnostics);
    }

    private static byte[] ExtractPages(byte[] content, int startPage, int endPage)
    {
        try
        {
            using var source = PdfReader.Open(new MemoryStream(content), PdfDocumentOpenMode.Import);
            using var output = new PdfDocument();
            for (var page = startPage - 1; page < endPage; page++)
                output.AddPage(source.Pages[page]);

            using var stream = new MemoryStream();
            output.Save(stream, false);
            return stream.ToArray();
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            throw new InvalidPartsPdfError(error);
        }
    }

    internal static string? NormalizePartName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.ToUpperInvariant();
    }
}

public sealed record PdfPageHeader(int PageNumber, string? NormalizedPartName, double Confidence);

public sealed record PdfPartSplitDiagnostic(int PageNumber, string? NormalizedPartName, double Confidence, string Code, string Message);

public sealed class PdfPartGroup(string normalizedPartName, int startPage, int endPage, double confidence, string diagnostic)
{
    public string NormalizedPartName { get; } = normalizedPartName;
    public int StartPage { get; } = startPage;
    public int EndPage { get; set; } = endPage;
    public double Confidence { get; set; } = confidence;
    public string Diagnostic { get; } = diagnostic;
    public byte[]? Content { get; set; }
}

public sealed record PdfPartSplitResult(IReadOnlyList<PdfPartGroup> Groups, IReadOnlyList<PdfPartSplitDiagnostic> Diagnostics)
{
    /// <summary>The original OCR headers before part-name extraction.</summary>
    public IReadOnlyList<PdfPageHeader> SourceHeaders { get; init; } = [];
}