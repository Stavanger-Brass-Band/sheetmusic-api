using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Errors;
using SheetMusic.Api.OData.MVC;
using SheetMusic.Api.Sets.Commands;
using SheetMusic.Api.Sets.Queries;
using SheetMusic.Api.Sets.RequestModels;
using SheetMusic.Api.Sets.Services;
using SheetMusic.Api.Sets.ViewModels;
using SheetMusic.Api.Users.Authorization;
using SheetMusic.Api.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.Sets;

/// <summary>
/// This controller contains endpoints for managing sheet music Set resources, including the parts (PDF files)
/// assigned to a set and the categories assigned to a set.
///
/// PS! Creating, updating and deleting sets, categories on a set and part content require Administrator privileges.
/// </summary>
[Authorize]
[ApiController]
[Route("sheetmusic")]
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
public class SheetMusicController(IBlobClient blobClient, IMemoryCache memoryCache, IMediator mediator, CatalogAccessService catalogAccess, SheetMusicAgent agent) : ControllerBase
{
    private const long MaxFileSize = 300000000L; //300 MB

    private const string SupportedSetExpand = "parts, projects";

    private static readonly object DownloadTokenLock = new();

    /// <summary>
    /// Gets complete list of sheet music sets, optionally expanding parts or projects, or the ones matching <paramref name="queryParams.Search"/> if provided.
    /// Use ZipDownloadUrl for complete parts download and PartsUrl to list parts.
    /// </summary>
    /// <param name="queryParams">Optional. OData support for $filter and $expand=parts,projects</param>
    /// <param name="category">Optional. Filter sets by category, identified by guid or name</param>
    /// <returns>Sets matching criteria</returns>
    /// <response code="200">A list of sets matching filter, or all sets. Empty list if no matching results</response>
    /// <response code="400">If an unsupported $expand value is provided</response>
    /// <response code="404">Category was not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    [Produces("application/json", Type = typeof(List<ApiSet>))]
    [HttpGet("sets")]
    public Task<IActionResult> GetSetList(ODataQueryParams queryParams, string? category) =>
        GetSetListInternal(queryParams, category);

    private async Task<IActionResult> GetSetListInternal(ODataQueryParams queryParams, string? category)
    {
        var unsupportedExpands = queryParams.Expand
            .Where(e => !SupportedSetExpand.Split(", ").Contains(e, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (unsupportedExpands.Count > 0)
            throw new InvalidQueryParametersError($"Unsupported $expand value(s): {string.Join(", ", unsupportedExpands)}. Supported values: {SupportedSetExpand}");

        var expandParts = queryParams.Expand.Any(e => string.Equals(e, "parts", StringComparison.OrdinalIgnoreCase));
        var expandProjects = queryParams.Expand.Any(e => string.Equals(e, "projects", StringComparison.OrdinalIgnoreCase));

        Guid? categoryId = null;

        if (!string.IsNullOrWhiteSpace(category))
        {
            var matchedCategory = await mediator.Send(new GetCategory(category));

            if (matchedCategory is null)
                return NotFound(new ProblemDetails { Detail = $"Category '{category}' was not found" });

            categoryId = matchedCategory.Id;
        }

        var matchingSets = await mediator.Send(new GetSets(queryParams, categoryId, expandProjects));
        var accessibleSetIds = await catalogAccess.GetAccessibleSetIdsAsync(matchingSets.Select(set => set.Id));

        var transformed = matchingSets.Where(set => accessibleSetIds.Contains(set.Id)).Select(s => new ApiSet(s)
            {
                ZipDownloadUrl = $"{BaseUrl}/sets/{s.Id}/zip",
                PartsUrl = $"{BaseUrl}/sets/{s.Id}/parts",
                Parts = expandParts ?
                    s.Parts.Select(p => new ApiSheetMusicPart(p)
                    {
                        PdfDownloadUrl = $"{BaseUrl}/sets/{p.SetId}/parts/{p.MusicPartId}/pdf",
                        DeletePartUrl = $"{BaseUrl}/sets/{p.SetId}/parts/{p.MusicPartId}"
                    }).ToList()
                    : null,
                Projects = expandProjects
                    ? s.ProjectConnections
                        .Where(connection => catalogAccess.CanAccessProject(connection.Project.StartDate, connection.Project.EndDate))
                        .Select(connection => new ApiProjectSummary(connection.Project))
                        .ToList()
                    : null
            })
            .ToList();

        return new OkObjectResult(transformed);
    }

    /// <summary>
    /// Lists parts for set with <paramref name="identifier"/> 
    /// </summary>
    /// <param name="identifier">A value uniquely identifying set. Either guid, archive number or title</param>
    /// <returns>List of parts for set</returns>
    /// <response code="200">Set information including its parts</response>
    /// <response code="404">Set not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    [Produces("application/json", Type = typeof(ApiSet))]
    [HttpGet("sets/{identifier}/parts")]
    public async Task<ActionResult<ApiSet>> GetPartsForSet(string identifier)
    {
        var set = await mediator.Send(new GetSet(identifier));

        if (set is null)
            return NotFound(new ProblemDetails { Detail = $"Set '{identifier}' was not found" });

        if (!await catalogAccess.CanAccessSetAsync(set.Id))
            return Forbid();

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
    /// <response code="200">The part matching the identifiers</response>
    /// <response code="404">Set, part, or the relationship between them was not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    [Produces("application/json", Type = typeof(ApiSheetMusicPart))]
    [HttpGet("sets/{setIdentifier}/parts/{partIdentifier}")]
    public async Task<IActionResult> GetSinglePart(string setIdentifier, string partIdentifier)
    {
        var partOnSet = await mediator.Send(new GetPartOnSet(setIdentifier, partIdentifier));

        if (partOnSet == null)
            return NotFound(new ProblemDetails { Detail = $"Relationship between '{setIdentifier}' and '{partIdentifier}' was not found" });

        if (!await catalogAccess.CanAccessSetAsync(partOnSet.SetId))
            return Forbid();

        return new OkObjectResult(new ApiSheetMusicPart(partOnSet));
    }

    /// <summary>
    /// Gets the PDF file for set with <paramref name="setIdentifier"/>, part with <paramref name="partIdentifier"/>
    /// </summary>
    /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title</param>
    /// <param name="partIdentifier">A value uniquely identifying part. Either guid or part name</param>
    /// <param name="downloadToken">A token to prove you are authorized for download</param>
    /// <returns>The PDF file, if it exists. 404 otherwise.</returns>
    /// <response code="200">The PDF file content</response>
    /// <response code="400">If the download token is missing or invalid</response>
    /// <response code="404">Set, part, or the relationship between them was not found</response>
    [AllowAnonymous]
    [Produces("application/pdf")]
    [HttpGet("sets/{setIdentifier}/parts/{partIdentifier}/pdf")]
    public async Task<IActionResult> GetSinglePartFile(string setIdentifier, string partIdentifier, string downloadToken)
    {
        var partOnSet = await mediator.Send(new GetPartOnSet(setIdentifier, partIdentifier));

        if (partOnSet == null)
            return NotFound(new ProblemDetails { Detail = $"Relationship between '{setIdentifier}' and '{partIdentifier}' was not found" });

        if (string.IsNullOrEmpty(downloadToken) || !TryConsumeDownloadToken(partOnSet.SetId, downloadToken))
        {
            return new BadRequestObjectResult("Download token must be provided and valid");
        }

        var pdf = await blobClient.GetMusicPartContentAsync(new PartRelatedToSet(partOnSet.SetId, partOnSet.MusicPartId));

        return File(pdf, "application/pdf", $"{partOnSet.Part.Name}.pdf");
    }

    /// <summary>
    /// Gets information about a single set, either by guid, number or title.
    /// </summary>
    /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title</param>
    /// <returns>Set matching <paramref name="setIdentifier"/></returns>
    /// <response code="200">The set matching the identifier</response>
    /// <response code="404">Set not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    [Produces("application/json", Type = typeof(ApiSet))]
    [HttpGet("sets/{setIdentifier}")]
    public async Task<IActionResult> GetSetinformationByIdentifier(string setIdentifier)
    {
        var set = await mediator.Send(new GetSet(setIdentifier));

        if (set is null)
            return NotFound(new ProblemDetails { Detail = $"Set '{setIdentifier}' was not found" });

        if (!await catalogAccess.CanAccessSetAsync(set.Id))
            return Forbid();

        return new OkObjectResult(new ApiSet(set)
        {
            ZipDownloadUrl = $"{BaseUrl}/sets/{set.Id}/zip",
            PartsUrl = $"{BaseUrl}/sets/{set.Id}/parts"
        });
    }

    /// <summary>
    /// Asks the sheet-music agent a question about a set identified by its name.
    /// </summary>
    /// <param name="request">The set name and question for the agent.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The agent's answer grounded in the set metadata.</returns>
    /// <response code="200">The agent response.</response>
    /// <response code="401">Authorization header is invalid or missing.</response>
    /// <response code="403">The caller cannot access the requested set.</response>
    /// <response code="404">The set was not found.</response>
    [Produces("application/json", Type = typeof(ApiSetAgentResponse))]
    [HttpPost("agent/chat")]
    public async Task<ActionResult<ApiSetAgentResponse>> AskAgentAboutSet(SetAgentQuestionRequest request, CancellationToken cancellationToken)
    {
        var set = await mediator.Send(new GetSet(request.SetName), cancellationToken);
        if (set is null)
            return NotFound(new ProblemDetails { Detail = $"Set '{request.SetName}' was not found" });

        if (!await catalogAccess.CanAccessSetAsync(set.Id))
            return Forbid();

        return new ApiSetAgentResponse(await agent.AnswerSetQuestionAsync(set, request.Question, cancellationToken));
    }

    /// <summary>
    /// Updates information about a set. PS! All properties will be updated, omitted once are nulled out.
    /// </summary>
    /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title</param>
    /// <param name="request">Update set parameters</param>
    /// <returns>Updated set metadata</returns>
    /// <response code="200">The updated set</response>
    /// <response code="400">If provided input is invalid. Should include a body with ProblemDetails-formatted errors.</response>
    /// <response code="404">Set not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [Produces("application/json", Type = typeof(ApiSet))]
    [Authorize(AuthPolicy.ManageMusic)]
    [HttpPut("sets/{setIdentifier}")]
    public async Task<ActionResult<ApiSet>> UpdateSetInformation(string setIdentifier, SetRequest request)
    {
        var set = await mediator.Send(new GetSet(setIdentifier));

        if (set is null)
            return NotFound(new ProblemDetails { Detail = $"Set '{setIdentifier}' was not found" });

        await mediator.Send(new UpdateSetMetadata(set.Id, request));

        set = await mediator.Send(new GetSet(setIdentifier));

        if (set is null)
            throw new Exception("Set was null when retrieving after update");

        return new ApiSet(set);
    }

    /// <summary>
    /// Gets the categories assigned to set with <paramref name="setIdentifier"/>
    /// </summary>
    /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title</param>
    /// <returns>List of categories assigned to the set</returns>
    /// <response code="200">List of categories assigned to the set</response>
    /// <response code="404">Set not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    [Produces("application/json", Type = typeof(List<ApiCategory>))]
    [HttpGet("sets/{setIdentifier}/categories")]
    public async Task<IActionResult> GetCategoriesForSet(string setIdentifier)
    {
        var set = await mediator.Send(new GetSet(setIdentifier));

        if (set is null)
            return NotFound(new ProblemDetails { Detail = $"Set '{setIdentifier}' was not found" });

        if (!await catalogAccess.CanAccessSetAsync(set.Id))
            return Forbid();

        var categories = set.Categories.Where(c => c.Category != null).Select(c => new ApiCategory(c.Category)).ToList();

        return new OkObjectResult(categories);
    }

    /// <summary>
    /// Assigns a category to set with <paramref name="setIdentifier"/>
    /// </summary>
    /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title</param>
    /// <param name="request">The category to assign, identified by guid or name</param>
    /// <returns>The updated list of categories assigned to the set</returns>
    /// <response code="200">The updated list of categories assigned to the set</response>
    /// <response code="404">Set or category not found</response>
    /// <response code="409">The category is already assigned to the set</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [Produces("application/json", Type = typeof(List<ApiCategory>))]
    [Authorize(AuthPolicy.ManageMusic)]
    [HttpPost("sets/{setIdentifier}/categories")]
    public async Task<IActionResult> AssignCategoryToSet(string setIdentifier, AssignCategoryRequest request)
    {
        await mediator.Send(new AssignCategoryToSet(setIdentifier, request.CategoryIdentifier));

        var set = await mediator.Send(new GetSet(setIdentifier));

        if (set is null)
            throw new NotFoundError(setIdentifier, "Set was not found");

        var categories = set.Categories.Where(c => c.Category != null).Select(c => new ApiCategory(c.Category)).ToList();

        return new OkObjectResult(categories);
    }

    /// <summary>
    /// Removes a category from set with <paramref name="setIdentifier"/>
    /// </summary>
    /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title</param>
    /// <param name="categoryIdentifier">A value uniquely identifying category. Either guid or name</param>
    /// <returns>204 if successfull, 404 if set, category or the assignment was not found</returns>
    /// <response code="204">Category was removed from the set successfully</response>
    /// <response code="404">Set, category, or the assignment was not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [Authorize(AuthPolicy.ManageMusic)]
    [HttpDelete("sets/{setIdentifier}/categories/{categoryIdentifier}")]
    public async Task<ActionResult> RemoveCategoryFromSet(string setIdentifier, string categoryIdentifier)
    {
        await mediator.Send(new RemoveCategoryFromSet(setIdentifier, categoryIdentifier));

        return NoContent();
    }

    /// <summary>
    /// Authorized a set for download, allowing a single download for the one with the token.
    /// </summary>
    /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title</param>
    /// <returns>A one-time download token</returns>
    /// <response code="200">The generated download token</response>
    /// <response code="404">Set not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    [HttpGet("sets/{setIdentifier}/zip/token")]
    public async Task<IActionResult> GetDownloadToken(string setIdentifier)
    {
        var set = await mediator.Send(new GetSet(setIdentifier));

        if (set is null)
            return NotFound(new ProblemDetails { Detail = $"Set '{setIdentifier}' was not found" });

        if (!await catalogAccess.CanAccessSetAsync(set.Id))
            return Forbid();

        //generated token using cryptographic library, save to memory cache and verify on download
        var token = KeyGenerator.GetUniqueKey(64);
        memoryCache.Set(DownloadTokenCacheKey(token), set.Id, TimeSpan.FromMinutes(60));

        return new OkObjectResult(token);
    }

    /// <summary>
    /// Gets the part collection for a set as a zip file. 
    /// Accepts anonymous requests, but they must provide a download token that is validated to be able to download.
    /// </summary>
    /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title</param>
    /// <param name="downloadToken">A token for proving that user is allowed to download this set</param>
    /// <returns>Zipped collection of parts</returns>
    /// <response code="200">The zipped collection of parts</response>
    /// <response code="400">If the download token is missing or invalid</response>
    /// <response code="404">Set not found</response>
    [AllowAnonymous]
    [Produces("application/zip")]
    [HttpGet("sets/{setIdentifier}/zip")]
    public async Task<IActionResult> GetPartsForSetAzZip(string setIdentifier, string downloadToken)
    {
        var set = await mediator.Send(new GetSet(setIdentifier));

        if (set is null)
            return NotFound(new ProblemDetails { Detail = $"Set '{setIdentifier}' was not found" });

        if (string.IsNullOrEmpty(downloadToken) || !TryConsumeDownloadToken(set.Id, downloadToken))
        {
            return new BadRequestObjectResult("Download token must be provided and valid");
        }

        var zipStream = await mediator.Send(new GetPartsZipAsStream(setIdentifier));
        await zipStream.FlushAsync();
        zipStream.Position = 0;

        return File(zipStream, "application/zip", $"{set.Title}.zip");
    }

    /// <summary>
    /// Analyzes the assigned parts and compares them with the blob storage content. 
    /// If a non-empty file does not exists, the set is listed in results
    /// </summary>
    /// <returns>The sets with parts that are assigned, but a file is not present</returns>
    /// <response code="200">The sets with parts that are assigned, but a file is not present</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    [Produces("application/json")]
    [HttpGet("sets/withoutFiles")]
    public async Task<IActionResult> GetSetsThatHasPartsButNoFiles()
    {
        var queryParams = new ODataQueryParams();
        queryParams.Expand.Add("parts");

        var setsWithParts = await mediator.Send(new GetSets(queryParams));
        var accessibleSetIds = await catalogAccess.GetAccessibleSetIdsAsync(setsWithParts.Select(set => set.Id));
        var results = new List<ApiSet>();

        foreach (var setWithParts in setsWithParts.Where(set => accessibleSetIds.Contains(set.Id)))
        {
            var apiSet = new ApiSet(setWithParts);

            foreach (var part in setWithParts.Parts)
            {
                if (await blobClient.HasPdfFileAsync(new PartRelatedToSet(setWithParts.Id, part.MusicPartId)) == false)
                {
                    apiSet?.Parts?.Add(new ApiSheetMusicPart(part));
                }
            }

            if (apiSet?.Parts?.Any() ?? false)
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
    /// <param name="cancellationToken">Cancellation token for the upload operation.</param>
    /// <returns>200 if successfull, 404 if not found, 500 if something bad happens</returns>
    /// <response code="200">Parts were uploaded successfully</response>
    /// <response code="404">Set not found</response>
    /// <response code="409">A part in the zip file is already assigned to the set</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [Authorize(AuthPolicy.ManageMusic)]
    [HttpPost("sets/{identifier}")]
    public async Task<IActionResult> UploadPartsForSets(string identifier, IFormFile file, CancellationToken cancellationToken)
    {
        await blobClient.EnsureContainerExistsAsync();

        using (var stream = file.OpenReadStream())
        {
            await mediator.Send(new AddPartsContentForSet(identifier, stream), cancellationToken);
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
    /// <response code="200">Part content was added successfully</response>
    /// <response code="404">Set or part not found</response>
    /// <response code="409">The part is already added to the set</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [Authorize(AuthPolicy.ManageMusic)]
    [HttpPost("sets/{setIdentifier}/parts/{partIdentifier}/content")]
    [MapToApiVersion("1.0")]
    [Obsolete("Use version 2.0 of endpoint instead")]
    public async Task<IActionResult> AddPartContent(string setIdentifier, string partIdentifier, IFormFile file)
    {
        using var stream = file.OpenReadStream();
        await mediator.Send(new AddPartOnSet(setIdentifier, partIdentifier, stream));

        return Ok();
    }

    /// <summary>
    /// Adds the PDF content for <paramref name="partIdentifier"/> of set with <paramref name="setIdentifier"/>.
    /// </summary>
    /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title</param>
    /// <param name="partIdentifier">Name of the part to add</param>
    /// <returns>200 if successfull, 404 if not found, 500 if something bad happens</returns>
    /// <response code="200">Part content was added successfully</response>
    /// <response code="400">If the uploaded file is missing or invalid</response>
    /// <response code="404">Set or part not found</response>
    /// <response code="409">The part is already added to the set</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [Authorize(AuthPolicy.ManageMusic)]
    [DisableFormValueModelBinding]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    [Consumes("multipart/form-data")]
    [HttpPost("sets/{setIdentifier}/parts/{partIdentifier}/content")]
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> AddPartContent(string setIdentifier, string partIdentifier)
    {
        try
        {
            using var fileStream = await MultipartRequestHelper.ExtractSingleFileStreamFromRequestAsync(Request);
            await mediator.Send(new AddPartOnSet(setIdentifier, partIdentifier, fileStream));

            return new OkResult();
        }
        catch (MultipartFileError mfe)
        {
            return BadRequest(mfe.Message);
        }
    }

    /// <summary>
    /// Changes an existing part assignment on a set to the selected replacement part.
    /// </summary>
    /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title.</param>
    /// <param name="partIdentifier">A value uniquely identifying the currently assigned part. Either guid or part name.</param>
    /// <param name="request">The selected replacement part.</param>
    /// <returns>The updated set-part assignment.</returns>
    /// <response code="200">The part assignment was changed successfully.</response>
    /// <response code="400">The replacement part identifier is missing or invalid.</response>
    /// <response code="404">Set, current part, replacement part, or the relationship between them was not found.</response>
    /// <response code="409">The replacement part is already assigned to the set.</response>
    /// <response code="401">Authorization header is invalid or missing.</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator).</response>
    [Authorize(AuthPolicy.ManageMusic)]
    [Produces("application/json", Type = typeof(ApiSheetMusicPart))]
    [HttpPut("sets/{setIdentifier}/parts/{partIdentifier}")]
    [MapToApiVersion("2.0")]
    public async Task<ActionResult<ApiSheetMusicPart>> ChangePart(string setIdentifier, string partIdentifier, ChangePartRequest request)
    {
        await mediator.Send(new ChangePartOnSet(setIdentifier, partIdentifier, request.PartIdentifier));
        var changedPart = await mediator.Send(new GetPartOnSet(setIdentifier, request.PartIdentifier));

        if (changedPart is null)
            throw new NotFoundError($"{setIdentifier}/{request.PartIdentifier}", "Changed part assignment was not found");

        return new ApiSheetMusicPart(changedPart);
    }

    /// <summary>Creates a new set and imports recognized parts from a combined score PDF.</summary>
    /// <param name="cancellationToken">Cancellation token for the split operation.</param>
    /// <returns>The created set.</returns>
    /// <response code="200">The set and its recognized parts were created.</response>
    /// <response code="400">The uploaded file is missing or invalid.</response>
    /// <response code="401">Authorization header is invalid or missing.</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator).</response>
    /// <response code="422">A set title could not be extracted from the PDF.</response>
    /// <response code="503">Document Intelligence OCR is not configured.</response>
    [Authorize(AuthPolicy.ManageMusic)]
    [DisableFormValueModelBinding]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    [Consumes("multipart/form-data")]
    [HttpPost("sets/pdf")]
    [MapToApiVersion("2.0")]
    public async Task<ActionResult<ApiSet>> CreateSetFromPdf(CancellationToken cancellationToken)
    {
        try
        {
            using var pdfContent = await MultipartRequestHelper.ExtractSingleFileStreamFromRequestAsync(Request);
            var set = await mediator.Send(new CreateSetFromPdf(pdfContent), cancellationToken);
            return new ApiSet(set);
        }
        catch (MultipartFileError error)
        {
            return BadRequest(error.Message);
        }
    }

    /// <summary>Imports recognized parts from a combined score PDF into an existing set.</summary>
    /// <param name="setId">The target set identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the import operation.</param>
    /// <returns>No content when the import completes.</returns>
    /// <response code="204">Recognized parts were added to the set.</response>
    /// <response code="400">The uploaded file is missing or invalid.</response>
    /// <response code="401">Authorization header is invalid or missing.</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator).</response>
    /// <response code="404">The set was not found.</response>
    /// <response code="503">Document Intelligence OCR is unavailable.</response>
    [Authorize(AuthPolicy.ManageMusic)]
    [DisableFormValueModelBinding]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    [Consumes("multipart/form-data")]
    [HttpPost("sets/{setId:guid}/parts/pdf")]
    [MapToApiVersion("2.0")]
    public async Task<ActionResult> AddPartsFromPdf(Guid setId, CancellationToken cancellationToken)
    {
        try
        {
            using var pdfContent = await MultipartRequestHelper.ExtractSingleFileStreamFromRequestAsync(Request);
            await mediator.Send(new AddPartsFromPdf(setId, pdfContent), cancellationToken);
            return NoContent();
        }
        catch (MultipartFileError error)
        {
            return BadRequest(error.Message);
        }
    }

    /// <summary>
    /// Deletes the PDF content and the relationship for <paramref name="partIdentifier"/> of set with <paramref name="setIdentifier"/>.
    /// </summary>
    /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title</param>
    /// <param name="partIdentifier">Name of the part to add</param>
    /// <returns>204 if successfull, 404 if not found, 500 if something bad happens</returns>
    /// <response code="204">Part content and relationship were deleted successfully</response>
    /// <response code="404">The relationship between the set and part was not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [Authorize(AuthPolicy.ManageMusic)]
    [HttpDelete("sets/{setIdentifier}/parts/{partIdentifier}")]
    public async Task<ActionResult> DeletePart(string setIdentifier, string partIdentifier)
    {
        await mediator.Send(new DeletePartOnSet(setIdentifier, partIdentifier));

        return NoContent();
    }

    /// <summary>
    /// Adds a new set to the list (without parts). 
    /// ID, number and scanned is optional. Number will be next in sequence if not specified.
    /// </summary>
    /// <param name="request">Information about the new set</param>
    /// <returns>200 if ok, 500 if something bad happens</returns>
    /// <response code="200">The newly created set</response>
    /// <response code="400">If provided input is invalid. Should include a body with ProblemDetails-formatted errors.</response>
    /// <response code="409">The requested archive number is already in use</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [Produces("application/json", Type = typeof(ApiSet))]
    [Authorize(AuthPolicy.ManageMusic)]
    [HttpPost("sets")]
    public async Task<IActionResult> AddNewSet([FromBody] SetRequest request)
    {
        if (request == null)
            return new BadRequestObjectResult("Please provide set information when creating a new set");

        await mediator.Send(new AddSet(request));

        var set = await mediator.Send(new GetSet(request.Title));

        if (set is null)
            throw new Exception("Newly added set was not found");

        return new OkObjectResult(new ApiSet(set));
    }

    /// <summary>
    /// Deletes the set with <paramref name="setIdentifier"/>, including all the parts and files
    /// </summary>
    /// <param name="setIdentifier">A value uniquely identifying set. Either guid, archive number or title</param>
    /// <returns>200 if ok, 404 if not found, 500 if something bad happens</returns>
    /// <response code="200">Set was deleted successfully</response>
    /// <response code="404">Set not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [Authorize(AuthPolicy.ManageMusic)]
    [HttpDelete("sets/{setIdentifier}")]
    public async Task<IActionResult> DeleteSet(string setIdentifier)
    {
        await mediator.Send(new DeleteSet(setIdentifier));

        return new OkResult();
    }

    private string BaseUrl => $"{Request.Scheme}://{Request.Host}/sheetmusic";

    private static string DownloadTokenCacheKey(string token) => $"Download_{token}";

    private bool TryConsumeDownloadToken(Guid setId, string providedToken)
    {
        lock (DownloadTokenLock)
        {
            if (memoryCache.TryGetValue(DownloadTokenCacheKey(providedToken), out Guid tokenSetId) && tokenSetId == setId)
            {
                memoryCache.Remove(DownloadTokenCacheKey(providedToken));
                return true;
            }
        }
        return false;
    }
}
