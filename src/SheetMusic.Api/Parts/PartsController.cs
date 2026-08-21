using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheetMusic.Api.Errors;
using SheetMusic.Api.OData.MVC;
using SheetMusic.Api.Parts.Commands;
using SheetMusic.Api.Parts.Errors;
using SheetMusic.Api.Parts.Queries;
using SheetMusic.Api.Parts.RequestModels;
using SheetMusic.Api.Parts.ViewModels;
using SheetMusic.Api.Users.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.Parts;

/// <summary>
/// This controller contains endpoints for manipulating Parts resources. 
/// Not to be confused with parts that belongs to a set, that is a different resource.
/// 
/// PS! Only Noteansvarlig and Administrators can invoke endpoints in this controller,
/// except rebuilding the part index which is Administrator only.
/// </summary>
[ApiController]
[Authorize(AuthPolicy.ManageMusic)]
[Produces("application/json")]
public class PartsController(IMediator mediator) : ControllerBase
{
    private const string SupportedPartExpand = "aliases";

    /// <summary>
    /// Rebuild the part index manually. 
    /// Whenever a change is done that requires rebuild, a rebuild is triggered. 
    /// This needn't be invoked unless you did some manual changes in the database.
    /// Requires Administrator privileges.
    /// </summary>
    /// <response code="204">Index was rebuilt successfully</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpPost("parts/index")]
    public async Task<ActionResult> BuildPartIndex()
    {
        await mediator.Send(new BuildPartIndex());

        return NoContent();
    }

    /// <summary>
    /// Gets a list of all Parts. OData filtering is supported, e.g. $filter=name eq 'partitur'.
    /// Use $expand=aliases to include the enabled aliases of each part.
    /// Requires Noteansvarlig or Administrator privileges.
    /// </summary>
    /// <param name="queryParams">The OData query paramateres specified</param>
    /// <response code="200">A list of parts matching filter, or all parts. Empty list if no matching results</response>
    /// <response code="400">If an unsupported $expand value is provided</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [HttpGet("parts")]
    public async Task<ActionResult<List<ApiPart>>> GetPartList([FromQuery] ODataQueryParams queryParams)
    {
        var unsupportedExpands = queryParams.Expand
            .Where(e => !string.Equals(e, SupportedPartExpand, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (unsupportedExpands.Count > 0)
            throw new InvalidQueryParametersError($"Unsupported $expand value(s): {string.Join(", ", unsupportedExpands)}. Supported values: {SupportedPartExpand}");

        var results = await mediator.Send(new GetPartCollection(queryParams));
        var apiModels = results.Select(p => new ApiPart(p));

        return apiModels.ToList();
    }

    /// <summary>
    /// Search for a part through the Part index.
    /// Requires Noteansvarlig or Administrator privileges.
    /// </summary>
    /// <param name="searchTerm">The search term to use when searching</param>
    /// <response code="200">The best match for the part</response>
    /// <response code="404">Part not found, make sure part name is correct</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [HttpGet("parts/index")]
    public async Task<ActionResult<ApiPart>> SearchForPartInIndex(string searchTerm)
    {
        var part = await mediator.Send(new SearchForPart(searchTerm));
        if (part == null) return NotFound();

        return new ApiPart(part);
    }

    /// <summary>
    /// Add a new part.
    /// Requires Noteansvarlig or Administrator privileges.
    /// </summary>
    /// <param name="request">Details about the new part</param>
    /// <response code="200">Details about the newly created part</response>
    /// <response code="400">If provided input is invalid. Should include a body with ProplemDetails-formatted errors.</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [HttpPost("parts")]
    public async Task<ActionResult<ApiPart>> AddNewPart(PartRequest request)
    {
        var command = new AddPart(request.Name, request.SortOrder, request.Indexable ?? false, request.AlwaysDisplay ?? false, request.InstrumentGroup);
        var part = await mediator.Send(command);

        if (part is null)
            return StatusCode(500); //newly created part not retrieved and error not detected, not very likely but an internal server error 

        return new ApiPart(part);
    }

    /// <summary>
    /// Gets details about part identified by <paramref name="partIdentifier"/>. 
    /// Requires Noteansvarlig or Administrator privileges.
    /// </summary>
    /// <param name="partIdentifier">Identifier (name, alias or Guid) of the part</param>
    /// <response code="200">Part information including list of aliases</response>
    /// <response code="404">Part not found, make sure part name is correct</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [HttpGet("parts/{partIdentifier}")]
    public async Task<ActionResult<ApiPart>> GetPart(string partIdentifier)
    {
        var part = await mediator.Send(new GetMusicPart(partIdentifier));

        if (part is null)
            return NotFound(new ProblemDetails { Detail = $"Part '{partIdentifier}' was not found" });

        return new ApiPart(part);
    }

    /// <summary>
    /// Updates information about part identified by <paramref name="partIdentifier"/> PS! Provide all values, those not provided will be set to null.
    /// Requires Noteansvarlig or Administrator privileges.
    /// </summary>
    /// <param name="partIdentifier">Identifier (name, alias or Guid) of the part</param>
    /// <param name="request">Request body containing all properties</param>
    /// <response code="200">Updated part information including list of aliases</response>
    /// <response code="404">Part not found, make sure part name is correct</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [HttpPut("parts/{partIdentifier}")]
    public async Task<ActionResult<ApiPart>> UpdatePart(string partIdentifier, PartRequest request)
    {
        var part = await mediator.Send(new GetMusicPart(partIdentifier));

        if (part is null)
            return NotFound(new ProblemDetails { Detail = $"Part '{partIdentifier}' not found" });

        var command = new UpdatePart(part.Id, request.Name, request.SortOrder, request.Indexable.GetValueOrDefault(false), request.AlwaysDisplay ?? part.AlwaysDisplay, request.InstrumentGroup);
        await mediator.Send(command);

        part = await mediator.Send(new GetMusicPart(partIdentifier));

        if (part is null)
            return StatusCode(500); //newly created part not retrieved and error not detected, not very likely but an internal server error 

        return new ApiPart(part);
    }

    /// <summary>
    /// Deletes part identified by <paramref name="partIdentifier"/> permanently. 
    /// Requires Noteansvarlig or Administrator privileges.
    /// </summary>
    /// <param name="partIdentifier">Identifier (name, alias or Guid) of the part. Case insensitive for part names.</param>
    /// <response code="204">Part and connected aliases are deleted</response>
    /// <response code="404">Part not found, make sure part name is correct</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [HttpDelete("parts/{partIdentifier}")]
    public async Task<ActionResult> DeletePart(string partIdentifier)
    {
        var part = await mediator.Send(new GetMusicPart(partIdentifier));

        if (part is null)
            return NotFound(new ProblemDetails { Detail = $"Part '{partIdentifier}' was not found" });

        await mediator.Send(new DeletePart(part.Id));

        return NoContent();
    }

    /// <summary>
    /// Adds <paramref name="alias"/> to part identified by <paramref name="partIdentifier"/>.
    /// Requires Noteansvarlig or Administrator privileges.
    /// </summary>
    /// <param name="partIdentifier">Identifier (name, alias or Guid) of the part</param>
    /// <param name="alias">An alias the part is also known as</param>
    /// <response code="200">Updated part information including list of aliases</response>
    /// <response code="404">Part not found, make sure part name is correct</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [HttpPost("parts/{partIdentifier}/aliases")]
    public async Task<ActionResult<ApiPart>> AddAlias(string partIdentifier, string alias)
    {
        var part = await mediator.Send(new GetMusicPart(partIdentifier));

        if (part is null)
            return NotFound(new ProblemDetails { Detail = $"Part '{partIdentifier}' was not found" });

        try
        {
            await mediator.Send(new AddAliasToPart(part.Id, alias));
        }
        catch (AliasAlreadyAddedError error)
        {
            return Conflict(new ProblemDetails { Detail = error.Message });
        }

        part = await mediator.Send(new GetMusicPart(partIdentifier));

        if (part is null)
            return StatusCode(500); //newly created part not retrieved and error not detected, not very likely but an internal server error 

        return new ApiPart(part);
    }

    /// <summary>
    /// Delete <paramref name="alias"/> from part identified by <paramref name="partIdentifier"/>.
    /// Requires Noteansvarlig or Administrator privileges.
    /// </summary>
    /// <param name="partIdentifier">Identifier (name, alias or Guid) of the part</param>
    /// <param name="alias">An alias the part is also known as</param>
    /// <response code="200">Updated part information including list of aliases</response>
    /// <response code="404">Part not found, make sure part name is correct</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Noteansvarlig or Administrator)</response>
    [HttpDelete("parts/{partIdentifier}/aliases/{alias}")]
    public async Task<ActionResult<ApiPart>> DeleteAliasFromPart(string partIdentifier, string alias)
    {
        var part = await mediator.Send(new GetMusicPart(partIdentifier));

        if (part is null)
            return NotFound(new ProblemDetails { Detail = $"Part '{partIdentifier}' was not found" });

        await mediator.Send(new RemoveAliasFromPart(part.Id, alias));

        part = await mediator.Send(new GetMusicPart(partIdentifier));

        if (part is null)
            return StatusCode(500); //newly created part not retrieved and error not detected, not very likely but an internal server error 

        return new ApiPart(part);
    }
}
