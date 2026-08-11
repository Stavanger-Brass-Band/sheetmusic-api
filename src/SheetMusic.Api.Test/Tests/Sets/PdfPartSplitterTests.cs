using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using PdfSharp.Pdf;
using SheetMusic.Api.Sets.Errors;
using SheetMusic.Api.Sets.Services;
using Xunit;

namespace SheetMusic.Api.Test.Tests.Sets;

public sealed class PdfPartSplitterTests
{
    [Fact]
    public void IsInHeader_IncludesWordsAcrossTheTopBand_AndExcludesMusicBelowIt()
    {
        var upperLeftWord = new float[] { 10, 20, 100, 20, 100, 40, 10, 40 };
        var upperRightWord = new float[] { 900, 20, 990, 20, 990, 40, 900, 40 };
        var boundaryWord = new float[] { 10, 150, 100, 150, 100, 160, 10, 160 };
        var musicWord = new float[] { 10, 200, 100, 200, 100, 220, 10, 220 };

        AzureDocumentIntelligencePageHeaderRecognizer.IsInHeader(upperLeftWord, width: 1000, height: 1000).Should().BeTrue();
        AzureDocumentIntelligencePageHeaderRecognizer.IsInHeader(upperRightWord, width: 1000, height: 1000).Should().BeTrue();
        AzureDocumentIntelligencePageHeaderRecognizer.IsInHeader(boundaryWord, width: 1000, height: 1000).Should().BeTrue();
        AzureDocumentIntelligencePageHeaderRecognizer.IsInHeader(musicWord, width: 1000, height: 1000).Should().BeFalse();
    }

    [Fact]
    public async Task SplitAsync_UsesOcrRecognizedHeaders_ToGroupAndExportPages()
    {
        var recognizer = new StubPageHeaderRecognizer([
            new PdfPageHeader(1, "FLUTE", 0.9),
            new PdfPageHeader(2, "FLUTE", 0.9),
            new PdfPageHeader(3, "TUBA", 0.9),
        ]);
        var splitter = new PdfPartSplitter(recognizer, new StubPartNameExtractor());

        await using var source = CreatePdf(pageCount: 3);
        var result = await splitter.SplitAsync(source, CancellationToken.None);

        recognizer.WasCalled.Should().BeTrue();
        result.Groups.Should().HaveCount(2);
        result.Groups[0].Content.Should().NotBeNullOrEmpty();
        result.Groups[1].Content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SplitAsync_StartsNewGroup_WhenNormalizedPartNameChanges()
    {
        var result = await SplitAsync([
            new PdfPageHeader(1, "CORNET 1", 1),
            new PdfPageHeader(2, "cornet 1", 1),
            new PdfPageHeader(3, "Trombone", 1),
        ]);

        result.Groups.Should().HaveCount(2);
        result.Groups[0].NormalizedPartName.Should().Be("CORNET 1");
        result.Groups[0].StartPage.Should().Be(1);
        result.Groups[0].EndPage.Should().Be(2);
        result.Groups[1].NormalizedPartName.Should().Be("TROMBONE");
        result.Groups[1].StartPage.Should().Be(3);
    }

    [Fact]
    public async Task SplitAsync_GroupsPages_WhenPartExtractorRemovesHeaderMetadata()
    {
        var result = await SplitAsync([
            new PdfPageHeader(1, "OVERTURE COMPOSER A FLUTE 1", 1),
            new PdfPageHeader(2, "OVERTURE COMPOSER A FLUTE 1", 1),
            new PdfPageHeader(3, "OVERTURE COMPOSER A TUBA", 1),
        ], new StubPartNameExtractor("FLUTE 1", "TUBA"));

        result.Groups.Should().HaveCount(2);
        result.Groups[0].NormalizedPartName.Should().Be("FLUTE 1");
        result.Groups[0].EndPage.Should().Be(2);
        result.Groups[1].NormalizedPartName.Should().Be("TUBA");
    }

    [Fact]
    public async Task SplitAsync_CreatesReviewGroup_WhenPartExtractorCannotExtractPart()
    {
        var result = await SplitAsync([
            new PdfPageHeader(1, "OVERTURE COMPOSER A FLUTE", 1),
            new PdfPageHeader(2, "OVERTURE COMPOSER A TUBA", 1),
            new PdfPageHeader(3, "OVERTURE COMPOSER A FLUTE", 1),
        ], new StubPartNameExtractor("FLUTE", null));

        result.Groups.Should().HaveCount(3);
        result.Groups[0].NormalizedPartName.Should().Be("FLUTE");
        result.Groups[1].NormalizedPartName.Should().Be("UNRECOGNIZED");
        result.Groups[2].NormalizedPartName.Should().Be("FLUTE");
    }

    [Fact]
    public async Task SplitAsync_CreatesReviewGroups_WhenExtractorReturnsWrongResultCount()
    {
        var result = await SplitAsync([
            new PdfPageHeader(1, "FLUTE", 1),
            new PdfPageHeader(2, "TUBA", 1),
        ], new WrongLengthPartNameExtractor());

        result.Groups.Should().ContainSingle();
        result.Groups[0].NormalizedPartName.Should().Be("UNRECOGNIZED");
        result.Groups[0].StartPage.Should().Be(1);
        result.Groups[0].EndPage.Should().Be(2);
    }

    [Fact]
    public async Task SplitAsync_RetainsRepeatedPages_InTheSamePartGroup()
    {
        var result = await SplitAsync([
            new PdfPageHeader(1, "EUPHONIUM", 1),
            new PdfPageHeader(2, "EUPHONIUM", 1),
            new PdfPageHeader(3, "EUPHONIUM", 1),
        ]);

        result.Groups.Should().ContainSingle();
        result.Groups[0].StartPage.Should().Be(1);
        result.Groups[0].EndPage.Should().Be(3);
    }

    [Fact]
    public async Task SplitAsync_CreatesReviewGroup_WhenHeaderIsUnreadable()
    {
        var result = await SplitAsync([
            new PdfPageHeader(1, "FLUTE", 1),
            new PdfPageHeader(2, null, 0),
            new PdfPageHeader(3, "FLUTE", 1),
        ]);

        result.Groups.Should().HaveCount(3);
        result.Groups[0].NormalizedPartName.Should().Be("FLUTE");
        result.Groups[0].EndPage.Should().Be(1);
        result.Groups[1].NormalizedPartName.Should().Be("UNRECOGNIZED");
        result.Groups[1].StartPage.Should().Be(2);
        result.Groups[2].NormalizedPartName.Should().Be("FLUTE");
        result.Groups[2].StartPage.Should().Be(3);
        result.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.PageNumber == 2 && diagnostic.Code == "HeaderUnreadable");
    }

    [Fact]
    public async Task SplitAsync_PreservesUnreadableFirstPage_InReviewGroup()
    {
        var result = await SplitAsync([
            new PdfPageHeader(1, null, 0),
            new PdfPageHeader(2, "FLUTE", 1),
        ]);

        result.Groups.Should().HaveCount(2);
        result.Groups[0].NormalizedPartName.Should().Be("UNRECOGNIZED");
        result.Groups[0].StartPage.Should().Be(1);
        result.Groups[0].Content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SplitAsync_PreservesPage_WhenOcrOmitsItsHeader()
    {
        var result = await SplitAsync([
            new PdfPageHeader(1, "FLUTE", 1),
            new PdfPageHeader(3, "TUBA", 1),
        ], pageCount: 3);

        result.Groups.Should().HaveCount(3);
        result.Groups[0].StartPage.Should().Be(1);
        result.Groups[0].EndPage.Should().Be(1);
        result.Groups[1].NormalizedPartName.Should().Be("UNRECOGNIZED");
        result.Groups[1].StartPage.Should().Be(2);
        result.Groups[2].NormalizedPartName.Should().Be("TUBA");
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.PageNumber == 2 && diagnostic.Code == "HeaderUnreadable");
    }

    [Fact]
    public async Task RecognizeAsync_ThrowsServiceUnavailable_WhenEndpointIsMissing()
    {
        var recognizer = new AzureDocumentIntelligencePageHeaderRecognizer(new ConfigurationBuilder().Build());

        var act = () => recognizer.RecognizeAsync(ReadOnlyMemory<byte>.Empty, CancellationToken.None);

        await act.Should().ThrowAsync<OcrConfigurationError>();
    }

    [Fact]
    public async Task SplitAsync_ThrowsBadRequest_WhenPdfIsInvalid()
    {
        var splitter = new PdfPartSplitter(new StubPageHeaderRecognizer([]), new StubPartNameExtractor());
        await using var invalidPdf = new MemoryStream([1, 2, 3]);

        var act = () => splitter.SplitAsync(invalidPdf, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidPartsPdfError>();
    }

    private static async Task<PdfPartSplitResult> SplitAsync(IReadOnlyList<PdfPageHeader> headers, IPdfPartNameExtractor? partNameExtractor = null, int? pageCount = null)
    {
        await using var source = CreatePdf(pageCount ?? headers.Count);
        return await new PdfPartSplitter(new StubPageHeaderRecognizer(headers), partNameExtractor ?? new StubPartNameExtractor()).SplitAsync(source, CancellationToken.None);
    }

    private static MemoryStream CreatePdf(int pageCount)
    {
        using var document = new PdfDocument();
        for (var page = 0; page < pageCount; page++)
            document.AddPage();

        var content = new MemoryStream();
        document.Save(content, false);
        content.Position = 0;
        return content;
    }

    private sealed class StubPageHeaderRecognizer(IReadOnlyList<PdfPageHeader> headers) : IPdfPageHeaderRecognizer
    {
        public bool WasCalled { get; private set; }

        public Task<IReadOnlyList<PdfPageHeader>> RecognizeAsync(ReadOnlyMemory<byte> pdfContent, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(headers);
        }
    }

    private sealed class StubPartNameExtractor(params string?[] partNames) : IPdfPartNameExtractor
    {
        public Task<IReadOnlyList<string?>> ExtractPartNamesAsync(IReadOnlyList<string> headerTexts, CancellationToken cancellationToken)
        {
            IReadOnlyList<string?> result = partNames.Length == 0
                ? headerTexts.Select(headerText => (string?)headerText).ToList()
                : headerTexts.Select((_, index) => partNames[Math.Min(index, partNames.Length - 1)]).ToList();
            return Task.FromResult(result);
        }
    }

    private sealed class WrongLengthPartNameExtractor : IPdfPartNameExtractor
    {
        public Task<IReadOnlyList<string?>> ExtractPartNamesAsync(IReadOnlyList<string> headerTexts, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string?>>(["FLUTE"]);
    }
}