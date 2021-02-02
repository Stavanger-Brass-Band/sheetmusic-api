using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.Controllers.ViewModels;
using SheetMusic.Api.CQRS.Command;
using SheetMusic.Api.CQRS.Queries;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.Errors;
using SheetMusic.Api.OData.MVC;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.Controllers
{
    /// <summary>
    /// This controller contains endpoints for manipulating Parts resources. 
    /// Not to be confused with parts that belongs to a set, that is a different resource.
    /// 
    /// PS! Only Administrators can invoke endpoints in this controller.
    /// </summary>
    [ApiController]
    [Authorize("Admin")]
    [Produces("application/json")]
    public class PartsController : ControllerBase
    {
        private readonly IMediator mediator;

        public PartsController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        /// <summary>
        /// Rebuild the part index manually. 
        /// Whenever a change is done that requires rebuild, a rebuild is triggered. 
        /// This needn't be invoked unless you did some manual changes in the database.
        /// Requires Administrator privileges.
        /// </summary>
        /// <response code="204">Index was rebuilt successfully</response>
        /// <response code="401">Authorization header (bearer token) is invalid</response>
        /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
        [HttpPost("parts/index")]
        public async Task<ActionResult> BuildPartIndex()
        {
            await mediator.Send(new BuildPartIndex());

            return NoContent();
        }

        /// <summary>
        /// Gets a list of all Parts. OData filtering is supported, e.g. $filter=name eq 'partitur'.
        /// Requires Administrator privileges.
        /// </summary>
        /// <param name="queryParams">The OData query paramateres specified</param>
        /// <response code="200">A list of parts matching filter, or all parts. Empty list if no matching results</response>
        /// <response code="401">Authorization header (bearer token) is invalid</response>
        /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
        [HttpGet("parts")]
        public async Task<ActionResult<List<ApiPart>>> GetPartList([FromQuery] ODataQueryParams queryParams)
        {
            var results = await mediator.Send(new GetPartCollection(queryParams));
            var apiModels = results.Select(p => new ApiPart(p));

            return apiModels.ToList();
        }

        /// <summary>
        /// Search for a part through the Part index.
        /// Requires Administrator privileges.
        /// </summary>
        /// <param name="searchTerm">The search term to use when searching</param>
        /// <response code="200">The best match for the part</response>
        /// <response code="404">Part not found, make sure part name is correct</response>
        /// <response code="401">Authorization header (bearer token) is invalid</response>
        /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
        [HttpGet("parts/index")]
        public async Task<ActionResult<ApiPart>> SearchForPartInIndex(string searchTerm)
        {
            var part = await mediator.Send(new SearchForPart(searchTerm));
            if (part == null) return NotFound();

            return new ApiPart(part);
        }

        /// <summary>
        /// Add a new part.
        /// Requires Administrator privileges.
        /// </summary>
        /// <param name="request">Details about the new part</param>
        /// <response code="200">Details about the newly created part</response>
        /// <response code="400">If provided input is invalid. Should include a body with ProplemDetails-formatted errors.</response>
        /// <response code="401">Authorization header (bearer token) is invalid</response>
        /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
        [HttpPost("parts")]
        public async Task<ActionResult<ApiPart>> AddNewPart(PartRequest request)
        {
            var command = new AddPart(request.Name, request.SortOrder, request.Indexable ?? true);
            await mediator.Send(command);

            var part = await mediator.Send(new GetMusicPart(request.Name));

            if (part is null)
                return StatusCode(500); //newly created part not retrieved and error not detected, not very likely but an internal server error 

            return new ApiPart(part);
        }

        /// <summary>
        /// Gets details about part identified by <paramref name="partIdentifier"/>. 
        /// Requires Administrator privileges.
        /// </summary>
        /// <param name="partIdentifier">Identifier (name, alias or Guid) of the part</param>
        /// <response code="200">Part information including list of aliases</response>
        /// <response code="404">Part not found, make sure part name is correct</response>
        /// <response code="401">Authorization header (bearer token) is invalid</response>
        /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
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
        /// Requires Administrator privileges.
        /// </summary>
        /// <param name="partIdentifier">Identifier (name, alias or Guid) of the part</param>
        /// <param name="request">Request body containing all properties</param>
        /// <response code="200">Updated part information including list of aliases</response>
        /// <response code="404">Part not found, make sure part name is correct</response>
        /// <response code="401">Authorization header (bearer token) is invalid</response>
        /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
        [HttpPut("parts/{partIdentifier}")]
        public async Task<ActionResult<ApiPart>> UpdatePart(string partIdentifier, PartRequest request)
        {
            var part = await mediator.Send(new GetMusicPart(partIdentifier));

            if (part is null)
                return NotFound(new ProblemDetails { Detail = $"Part '{partIdentifier}' not found" });

            var command = new UpdatePart(part.Id, request.Name, request.SortOrder, request.Indexable.GetValueOrDefault(false));
            await mediator.Send(command);

            part = await mediator.Send(new GetMusicPart(partIdentifier));

            if (part is null)
                return StatusCode(500); //newly created part not retrieved and error not detected, not very likely but an internal server error 

            return new ApiPart(part);
        }

        /// <summary>
        /// Deletes part identified by <paramref name="partIdentifier"/> permanently. 
        /// Requires Administrator privileges.
        /// </summary>
        /// <param name="partIdentifier">Identifier (name, alias or Guid) of the part. Case insensitive for part names.</param>
        /// <response code="204">Part and connected aliases are deleted</response>
        /// <response code="404">Part not found, make sure part name is correct</response>
        /// <response code="401">Authorization header (bearer token) is invalid</response>
        /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
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
        /// Requires Administrator privileges.
        /// </summary>
        /// <param name="partIdentifier">Identifier (name, alias or Guid) of the part</param>
        /// <param name="alias">An alias the part is also known as</param>
        /// <response code="200">Updated part information including list of aliases</response>
        /// <response code="404">Part not found, make sure part name is correct</response>
        /// <response code="401">Authorization header (bearer token) is invalid</response>
        /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
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
        /// Requires Administrator privileges.
        /// </summary>
        /// <param name="partIdentifier">Identifier (name, alias or Guid) of the part</param>
        /// <param name="alias">An alias the part is also known as</param>
        /// <response code="200">Updated part information including list of aliases</response>
        /// <response code="404">Part not found, make sure part name is correct</response>
        /// <response code="401">Authorization header (bearer token) is invalid</response>
        /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
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
}
