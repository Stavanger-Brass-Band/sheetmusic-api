using IronPdf;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SheetMusic.Api.Controllers.InternalModels;
using SheetMusic.Api.CQRS.Queries;
using SheetMusic.Api.Database.Entities;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.Controllers
{
    [Authorize]
    [Route("pdf")]
    public class PdfController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly IConfiguration configuration;
        private List<MusicPart> parts = new List<MusicPart>();

        public PdfController(IMediator mediator, IConfiguration configuration)
        {
            this.mediator = mediator;
            this.configuration = configuration;
        }

        /// <summary>
        /// Splits the PDF <paramref name="file"/> into single-page pdf files added to a zip archive
        /// </summary>
        /// <param name="file">The file to split</param>
        /// <returns>Zip file with the split results</returns>
        [HttpPost("singleSplit")]
        public async Task<IActionResult> SplitPdfIntoSinglePages(IFormFile file)
        {
            var doc = new PdfDocument(file.OpenReadStream());

            var memoryStream = new MemoryStream();

            using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                for (var i = 0; i < doc.PageCount; i++)
                {
                    var singlePage = doc.CopyPage(i);
                    var entryName = $"{file.Name}_{i}.pdf";
                    var zipEntry = zip.CreateEntry(entryName);

                    using (var entryStream = zipEntry.Open())
                    {
                        await entryStream.WriteAsync(singlePage.BinaryData);
                        await entryStream.FlushAsync();
                    }
                }
            }

            await memoryStream.FlushAsync();
            memoryStream.Position = 0;

            return File(memoryStream, "application/zip", "splitCollection.zip");
        }

        /// <summary>
        /// Attempts to recognize where the page splits are, splits the files and returns the pdf collection in a zip archive
        /// </summary>
        /// <param name="file">The file to split</param>
        /// <returns>Zip archive with results</returns>
        [HttpPost("smartSplit")]
        public async Task<IActionResult> SmartSplitPdf(IFormFile file)
        {
            var doc = new PdfDocument(file.OpenReadStream());
            var tempArea = Path.GetTempPath();
            var memoryStream = new MemoryStream();

            using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var pagesAsImages = doc.ToPngImages($"{tempArea}\\pdf_page_*.png");
                var partPageMappings = await MapPartPerPageAsync(pagesAsImages);
                var partPages = new List<PdfDocument>();
                string previousPart = partPageMappings[0];

                for (var i = 0; i < doc.PageCount; i++)
                {
                    var singlePage = doc.CopyPage(i);
                    var currentPart = partPageMappings[i];

                    if ((currentPart != null && currentPart != previousPart) || i == doc.PageCount - 1) //different part or end of set? Do not split if current is unidentified.
                    {
                        if (i == doc.PageCount) partPages.Add(singlePage); //make sure we include the last page

                        //new part, finish last one
                        var entryName = !string.IsNullOrWhiteSpace(previousPart) ? $"{previousPart}.pdf" : $"unidentified_{i}.pdf";

                        var zipEntry = zip.CreateEntry(entryName);

                        using (var entryStream = zipEntry.Open())
                        {
                            var firstPage = partPages.First();
                            partPages.Remove(firstPage);

                            foreach (var page in partPages)
                            {
                                firstPage.AppendPdf(page);
                            }

                            await entryStream.WriteAsync(firstPage.BinaryData);
                            await entryStream.FlushAsync();
                        }
                        partPages.Clear();
                    }

                    partPages.Add(singlePage);

                    if (currentPart != null)
                        previousPart = currentPart;
                }
            }

            await memoryStream.FlushAsync();
            memoryStream.Position = 0;

            return File(memoryStream, "application/zip", "splitCollection.zip");
        }

        private async Task<Dictionary<int, string>> MapPartPerPageAsync(string[] pagePaths)
        {
            var results = new Dictionary<int, string>();

            for (var i = 0; i < pagePaths.Count(); i++)
            {
                var currentAnalysisResult = await AnalyzeImageAsync(pagePaths[i]);
                results.Add(i, currentAnalysisResult.PartName);
            }

            return results;
        }

        private async Task<SheetPageInfo> AnalyzeImageAsync(string path)
        {
            var credentials = new ApiKeyServiceClientCredentials(configuration["CognitiveServices:Key"]);

            //upper middle = title
            //top left = part (low 1 and 2)
            //top right = composer / arranger

            //bounding box = 4 comma-separated numbers
            //1 = x coordinate on left
            //2 = y coordinate on top
            //3 = width
            //4 = height


            //boundaries
            //total width = 1200ish, total height = 1800ish (150 PPI A4)
            //title: x <= 300 - x >= 800. y <= 0
            //left: x <= 0, 

            //title = the top-most text in the document which is quite large and in the center-ish
            //part name = left text, quite small. Might be more parts.
            //composer/arranger = right text, even smaller.

            var result = new SheetPageInfo();

            using (var cvClient = new ComputerVisionClient(credentials) { Endpoint = configuration["CognitiveServices:Endpoint"] })
            using (var imageFileStream = System.IO.File.OpenRead(path))
            {
                var ocrResult = await cvClient.RecognizePrintedTextInStreamAsync(false, imageFileStream);
                var rawJson = JsonConvert.SerializeObject(ocrResult);

                //first, attempt to find a part match of any line
                foreach (var region in ocrResult.Regions)
                {
                    foreach (var line in region.Lines)
                    {
                        var words = line.Words.Select(o => o.Text).ToList();

                        var partName = string.Join(" ", words);

                        if (partName.Length >= 5) //at least 5 characters for part names to avoid confusion
                        {
                            var part = await mediator.Send(new SearchForPart(partName));

                            if (part != null)
                            {
                                result.PartName = part.Name!;
                                return result;
                            }
                            else
                            {
                                Debug.WriteLine($"No part found for {partName}");
                            }
                        }

                        var box = new BoundingBox(line.BoundingBox);

                        if (box.X >= 250 && box.X < 650 && box.Y <= 200 && box.Height >= 20) //middle-ish, topish with fairly big text. Title.
                        {
                            words = line.Words.Select(o => o.Text).ToList();
                            result.Title = string.Join(" ", words);
                        }
                        else if (box.X >= 650 && box.Y <= 200) //composer/arranger ??
                        {
                            words = line.Words.Select(o => o.Text).ToList();
                            var composer = string.Join(" ", words);
                            result.ComposerArranger = composer;
                        }
                    }
                }
            }

            //using (var cvClient = new ComputerVisionClient(credentials) { Endpoint = configuration["CognitiveServices:Endpoint"] })
            //using (var imageFileStream = System.IO.File.OpenRead(path))
            //{
            //    var handwrittenResult = await cvClient.RecognizeTextInStreamAsync(imageFileStream, TextRecognitionMode.Handwritten);
            //    string rawHandwrittenJson = JsonConvert.SerializeObject(handwrittenResult);
            //}

            return result;
        }
    }
}
