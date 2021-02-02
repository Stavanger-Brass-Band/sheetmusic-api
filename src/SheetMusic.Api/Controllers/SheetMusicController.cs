﻿using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using SheetMusic.Api.Authorization;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.Controllers.ViewModels;
using SheetMusic.Api.CQRS.Command;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Repositories;
using SheetMusic.Api.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("sheetmusic")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("2.0")]
    public class SheetMusicController : ControllerBase
    {
        private readonly IBlobClient blobClient;
        private readonly IPartRepository partRepository;
        private readonly ISetRepository setRepository;
        private readonly IMemoryCache memoryCache;
        private readonly IMediator mediator;

        private const long MaxFileSize = 300000000L; //300 MB

        public SheetMusicController(IBlobClient blobClient, IPartRepository partRepository, ISetRepository setRepository, IMemoryCache memoryCache, IMediator mediator)
        {
            this.blobClient = blobClient;
            this.partRepository = partRepository;
            this.setRepository = setRepository;
            this.memoryCache = memoryCache;
            this.mediator = mediator;
        }

        /// <summary>
        /// Gets complete list of sheet music sets (without parts), or the ones matching <paramref name="searchTerm"/> if provided
        /// Use ZipDownloadUrl for complete parts download and PartsUrl to list parts
        /// </summary>
        /// <param name="searchTerm">Optional. A search term, can be archive number, title, arranger or composer</param>
        /// <returns>Sets matching search term</returns>
        [Produces("application/json", Type = typeof(List<ApiSet>))]
        [HttpGet("sets")]
        public async Task<IActionResult> GetSetList(string? searchTerm)
        {
            List<SheetMusicSet> matchingSets;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                matchingSets = await setRepository.SearchAsync(searchTerm);
            }
            else
            {
                matchingSets = await setRepository.GetSetsAsync();
            }

            var transformed = matchingSets.Select(s => new ApiSet(s)
            {
                ZipDownloadUrl = $"{BaseUrl}/sets/{s.Id}/zip",
                PartsUrl = $"{BaseUrl}/sets/{s.Id}/parts"

            })
            .OrderBy(s => s.ArchiveNumber)
            .ToList();

            return new OkObjectResult(transformed);
        }

        /// <summary>
        /// Lists parts for set with <paramref name="identifier"/> 
        /// </summary>
        /// <param name="identifier">A value uniquely identifying set. Either guid, archive number or title</param>
        /// <returns>List of parts for set</returns>
        [Produces("application/json", Type = typeof(ApiSet))]
        [HttpGet("sets/{identifier}/parts")]
        public async Task<ActionResult<ApiSet>> GetPartsForSet(string identifier)
        {
            var set = await setRepository.ResolveByIdentiferAsync(identifier);
            var query = new GetPartsForSet(set.Id);
            var parts = await mediator.Send(query);

            var apiSet = new ApiSet(set)
            {
                ZipDownloadUrl = $"{BaseUrl}/sets/{set.Id}/zip",
                Parts = parts.Select(p => new ApiSheetMusicPart(p)
                {
                    PdfDownloadUrl = $"{BaseUrl}/sets/{p.SetId}/parts/{p.MusicPartId}/pdf",
                    DeletePartUrl = $"{BaseUrl}/sets/{p.SetId}/parts/{p.MusicPartId}"
                }).ToList()
            };

            return apiSet;
        }

        /// <summary>
        /// Get a single part for a set
        /// </summary>
        /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title</param>
        /// <param name="partIdentifier">A value uniquely identifying part. Either guid or part name</param>
        /// <returns>The part that matches, 404 if not found</returns>

        [Produces("application/json", Type = typeof(ApiSheetMusicPart))]
        [HttpGet("sets/{setIdentifier}/parts/{partIdentifier}")]
        public async Task<IActionResult> GetSinglePart(string setIdentifier, string partIdentifier)
        {
            var set = await setRepository.ResolveByIdentiferAsync(setIdentifier);
            var part = await mediator.Send(new GetMusicPart(partIdentifier));

            if (part is null)
                return NotFound(new ProblemDetails { Detail = $"Part '{partIdentifier}' was not found" });

            var result = await setRepository.GetMusicPartForSetAsync(set.Id, part.Id);

            if (result == null)
                return NotFound(new ProblemDetails { Detail = $"Relationship between '{setIdentifier}' and '{partIdentifier}' was not found" });

            return new OkObjectResult(new ApiSheetMusicPart(result));
        }

        /// <summary>
        /// Gets the PDF file for set with <paramref name="identifier"/>, part with <paramref name="partIdentifier"/>
        /// </summary>
        /// <param name="identifier">A value uniquely identifying set. Either guid, archive number or title</param>
        /// <param name="partIdentifier">A value uniquely identifying part. Either guid or part name</param>
        /// <param name="downloadToken">A token to prove you are authorized for download</param>
        /// <returns>The PDF file, if it exists. 404 otherwise.</returns>
        [AllowAnonymous]
        [Produces("application/pdf")]
        [HttpGet("sets/{identifier}/parts/{partIdentifier}/pdf")]
        public async Task<IActionResult> GetSinglePartFile(string identifier, string partIdentifier, string downloadToken)
        {
            var set = await setRepository.ResolveByIdentiferAsync(identifier);

            if (string.IsNullOrEmpty(downloadToken) || !TokenIsValid(set.Id, downloadToken))
            {
                return new BadRequestObjectResult("Download token must be provided and valid");
            }

            var part = await mediator.Send(new GetMusicPart(partIdentifier));

            if (part is null)
                return NotFound(new ProblemDetails { Detail = $"Part '{partIdentifier}' was not found" });

            var pdf = await blobClient.GetMusicPartContentAsync(new PartRelatedToSet(set.Id, part.Id));

            memoryCache.Remove(DownloadTokenCacheKey(set.Id)); //this is a one-time token
            return File(pdf, "application/pdf", $"{part.Name}.pdf");
        }

        /// <summary>
        /// Gets information about a single set, either by guid, number or title.
        /// </summary>
        /// <param name="identifier">A value uniquely identifying set. Either guid, archive number or title</param>
        /// <returns>Set matching <paramref name="identifier"/></returns>
        [Produces("application/json", Type = typeof(ApiSet))]
        [HttpGet("sets/{identifier}")]
        public async Task<IActionResult> GetSetinformationByIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return new BadRequestObjectResult("Identifier must be provided");
            }

            var set = await setRepository.ResolveByIdentiferAsync(identifier);

            return new OkObjectResult(new ApiSet(set)
            {
                ZipDownloadUrl = $"{BaseUrl}/sets/{set.Id}/zip",
                PartsUrl = $"{BaseUrl}/sets/{set.Id}/parts"
            });
        }

        /// <summary>
        /// Updates information about a set. PS! All properties will be updated, omitted once are nulled out.
        /// </summary>
        /// <param name="identifier">A value uniquely identifying set. Either guid, archive number or title</param>
        /// <param name="request">Update set parameters</param>
        /// <returns>Updated set metadata</returns>
        [Produces("application/json", Type = typeof(ApiSet))]
        [Authorize(AuthPolicy.Admin)]
        [HttpPut("sets/{identifier}")]
        public async Task<ActionResult<ApiSet>> UpdateSetInformation(string identifier, SetRequest request)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return new BadRequestObjectResult("Identifier must be provided");
            }

            var set = await setRepository.ResolveByIdentiferAsync(identifier);
            await mediator.Send(new UpdateSetMetadata(set.Id, request));

            set = await setRepository.ResolveByIdentiferAsync(identifier);

            return new ApiSet(set);
        }

        /// <summary>
        /// Authorized a set for download, allowing a single download for the one with the token.
        /// </summary>
        /// <param name="identifier">A value uniquely identifying set. Either guid, archive number or title</param>
        /// <returns></returns>
        [HttpGet("sets/{identifier}/zip/token")]
        public async Task<IActionResult> GetDownloadToken(string identifier)
        {
            var set = await setRepository.ResolveByIdentiferAsync(identifier);

            //generated token using cryptographic library, save to memory cache and verify on download
            var token = KeyGenerator.GetUniqueKey(64);
            memoryCache.Set(DownloadTokenCacheKey(set.Id), token, TimeSpan.FromMinutes(60));

            return new OkObjectResult(token);
        }

        /// <summary>
        /// Gets the part collection for a set as a zip file. 
        /// Accepts anonymous requests, but they must provide a download token that is validated to be able to download.
        /// </summary>
        /// <param name="identifier">A value uniquely identifying set. Either guid, archive number or title</param>
        /// <param name="downloadToken">A token for proving that user is allowed to download this set</param>
        /// <returns>Zipped collection of parts</returns>
        [AllowAnonymous]
        [Produces("application/zip")]
        [HttpGet("sets/{identifier}/zip")]
        public async Task<IActionResult> GetPartsForSetAzZip(string identifier, string downloadToken)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return new BadRequestObjectResult("Identifier must be provided");
            }

            var set = await setRepository.ResolveByIdentiferAsync(identifier);

            if (string.IsNullOrEmpty(downloadToken) || !TokenIsValid(set.Id, downloadToken))
            {
                return new BadRequestObjectResult("Download token must be provided and valid");
            }

            var zip = await setRepository.GetPartPdfsAsZipForSetAsync(identifier);
            await zip.FlushAsync();

            zip.Position = 0;
            memoryCache.Remove(DownloadTokenCacheKey(set.Id)); //this is a one-time token

            return File(zip, "application/zip", $"{set.Title}.zip");
        }

        /// <summary>
        /// Analyzes the assigned parts and compares them with the blob storage content. 
        /// If a non-empty file does not exists, the set is listed in results
        /// </summary>
        /// <returns>The sets with parts that are assigned, but a file is not present</returns>
        [Produces("application/json")]
        [HttpGet("sets/withoutFiles")]
        public async Task<IActionResult> GetSetsThatHasPartsButNoFiles()
        {
            var setsWithParts = await setRepository.GetSetsWithPartsAsync();
            var results = new List<ApiSet>();

            foreach (var setWithParts in setsWithParts)
            {
                var apiSet = new ApiSet(setWithParts);

                foreach (var part in setWithParts.Parts)
                {
                    if (await blobClient.HasPdfFileAsync(new PartRelatedToSet(setWithParts.Id, part.MusicPartId)) == false)
                    {
                        apiSet.Parts.Add(new ApiSheetMusicPart(part));
                    }
                }

                if (apiSet.Parts.Any())
                {
                    results.Add(apiSet);
                }
            }

            return new OkObjectResult(results);
        }

        /// <summary>
        /// Upload all parts for set with identifier <paramref name="identifier"/> as zip file
        /// </summary>
        /// <param name="identifier">A value uniquely identifying set. Either guid, archive number or title</param>
        /// <param name="file">The file that has all parts. Needs to be a zip file.</param>
        /// <returns>200 if successfull, 404 if not found, 500 if something bad happens</returns>
        [Authorize(AuthPolicy.Admin)]
        [HttpPost("sets/{identifier}")]
        public async Task<IActionResult> UploadPartsForSets(string identifier, IFormFile file)
        {
            await blobClient.EnsureContainerExistsAsync();

            using (var stream = file.OpenReadStream())
            {
                await setRepository.AddPartContentForSetAsync(identifier, file.OpenReadStream());
            }

            return new OkResult();
        }

        /// <summary>
        /// Adds the PDF content for <paramref name="partIdentifier"/> of set with <paramref name="setIdentifier"/>.
        /// </summary>
        /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title</param>
        /// <param name="partIdentifier">Name of the part to add</param>
        /// <param name="file">The PDF file for the part</param>
        /// <returns>200 if successfull, 404 if not found, 500 if something bad happens</returns>
        [Authorize(AuthPolicy.Admin)]
        [HttpPost("sets/{setIdentifier}/parts/{partIdentifier}/content")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> AddPartContent(string setIdentifier, string partIdentifier, IFormFile file)
        {
            var set = await setRepository.ResolveByIdentiferAsync(setIdentifier);
            var part = await mediator.Send(new GetMusicPart(partIdentifier));

            if (part is null)
                return NotFound(new ProblemDetails { Detail = $"Part {partIdentifier} was not found" });

            if (set.Parts.Any(p => p.MusicPartId == part.Id))
                throw new MusicSetPartAlreadyAddedError(set.Title, part.Name);

            var relationship = new PartRelatedToSet(set.Id, part.Id);

            await blobClient.AddMusicPartContentAsync(relationship, file.OpenReadStream());
            await setRepository.AddMusicPartForSetAsync(set.Id, part.Id);

            return Ok();
        }

        /// <summary>
        /// Adds the PDF content for <paramref name="partIdentifier"/> of set with <paramref name="setIdentifier"/>.
        /// </summary>
        /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title</param>
        /// <param name="partIdentifier">Name of the part to add</param>
        /// <returns>200 if successfull, 404 if not found, 500 if something bad happens</returns>
        [Authorize(AuthPolicy.Admin)]
        [DisableFormValueModelBinding]
        [RequestSizeLimit(MaxFileSize)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
        [Consumes("multipart/form-data")]
        [HttpPost("sets/{setIdentifier}/parts/{partIdentifier}/content")]
        [MapToApiVersion("2.0")]
        public async Task<IActionResult> AddPartContent(string setIdentifier, string partIdentifier)
        {
            var set = await setRepository.ResolveByIdentiferAsync(setIdentifier);
            var part = await mediator.Send(new GetMusicPart(partIdentifier));

            if (part is null)
                return NotFound(new ProblemDetails { Detail = $"Part {partIdentifier} was not found" });

            if (set.Parts.Any(p => p.MusicPartId == part.Id))
                throw new MusicSetPartAlreadyAddedError(set.Title, part.Name);

            var relationship = new PartRelatedToSet(set.Id, part.Id);

            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
                return BadRequest("Not a multipart request");

            var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType), 10000000);
            var reader = new MultipartReader(boundary, Request.Body);

            // note: this is for a single file, you could also process multiple files
            var section = await reader.ReadNextSectionAsync();

            if (section == null)
                return BadRequest("No sections in multipart defined");

            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                return BadRequest("No content disposition in multipart defined");

            var fileName = contentDisposition.FileNameStar.ToString();
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = contentDisposition.FileName.ToString();
            }

            if (string.IsNullOrEmpty(fileName))
                return BadRequest("No filename defined.");

            using var fileStream = section.Body;

            await blobClient.AddMusicPartContentAsync(relationship, fileStream);
            await setRepository.AddMusicPartForSetAsync(set.Id, part.Id);

            return new OkResult();
        }

        /// <summary>
        /// Deletes the PDF content and the relationship for <paramref name="partIdentifier"/> of set with <paramref name="identifier"/>.
        /// </summary>
        /// <param name="identifier">A value uniquely identifying set. Either guid, archive number or title</param>
        /// <param name="partIdentifier">Name of the part to add</param>
        /// <returns>204 if successfull, 404 if not found, 500 if something bad happens</returns>
        [Authorize(AuthPolicy.Admin)]
        [HttpDelete("sets/{identifier}/parts/{partIdentifier}")]
        public async Task<ActionResult> DeletePart(string identifier, string partIdentifier)
        {
            var set = await setRepository.ResolveByIdentiferAsync(identifier);
            var part = await mediator.Send(new GetMusicPart(partIdentifier));

            if (part is null)
                return NotFound(new ProblemDetails { Detail = $"Part '{partIdentifier}' was not found" });

            var blobIdentifer = new PartRelatedToSet(set.Id, part.Id);

            await blobClient.DeletePartContentAsync(blobIdentifer);
            await setRepository.DeleteMusicPartForSetAsync(set.Id, part.Id);

            return NoContent();
        }

        /// <summary>
        /// Adds a new set to the list (without parts). 
        /// ID, number and scanned is optional. Number will be next in sequence if not specified.
        /// </summary>
        /// <param name="request">Information about the new set</param>
        /// <returns>200 if ok, 500 if something bad happens</returns>
        [Produces("application/json", Type = typeof(ApiSet))]
        [Authorize(AuthPolicy.Admin)]
        [HttpPost("sets")]
        public async Task<IActionResult> AddNewSet([FromBody] SetRequest request)
        {
            if (request == null)
                return new BadRequestObjectResult("Please provide set information when creating a new set");

            var archiveNumber = request.ArchiveNumber ?? await setRepository.GetNextAvailableArchiveNumberAsync();

            var set = new SheetMusicSet(archiveNumber, request.Title)
            {
                Composer = request.Composer,
                Arranger = request.Arranger,
                SoleSellingAgent = request.SoleSellingAgent,
                MissingParts = request.MissingParts,
                BorrowedFrom = request.BorrowedFrom,
                BorrowedDateTime = string.IsNullOrEmpty(request.BorrowedFrom) ? null : (DateTimeOffset?)DateTimeOffset.Now
            };

            await setRepository.AddNewSetAsync(set);

            return new OkObjectResult(new ApiSet(set));
        }

        /// <summary>
        /// Deletes the set with <paramref name="identifier"/>, including all the parts and files
        /// </summary>
        /// <param name="identifier">A value uniquely identifying set. Either guid, archive number or title</param>
        /// <returns>200 if ok, 404 if not found, 500 if something bad happens</returns>
        [Authorize(AuthPolicy.Admin)]
        [HttpDelete("sets/{identifier}")]
        public async Task<IActionResult> DeleteSet(string identifier)
        {
            var set = await setRepository.ResolveByIdentiferAsync(identifier);
            await setRepository.DeleteSetAsync(set);

            return new OkResult();
        }

        private string BaseUrl => $"{Request.Scheme}://{Request.Host}/sheetmusic";

        private string DownloadTokenCacheKey(Guid setId)
        {
            return $"Download_{setId}";
        }

        private bool TokenIsValid(Guid setId, string providedToken)
        {
            if (memoryCache.TryGetValue(DownloadTokenCacheKey(setId), out string cachedToken))
            {
                if (providedToken == cachedToken)
                {
                    return true;
                }
            }

            return false;
        }
    }
}