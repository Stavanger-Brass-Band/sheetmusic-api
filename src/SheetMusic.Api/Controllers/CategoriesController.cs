using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.Controllers.ViewModels;
using SheetMusic.Api.CQRS.Command;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.Errors;
using SheetMusic.Api.OData.MVC;
using SheetMusic.Api.Users.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.Controllers;

/// <summary>
/// This controller contains endpoints for managing Category resources.
/// Categories are used to tag sheet music sets.
///
/// PS! Creating, updating and deleting categories require Administrator privileges.
/// </summary>
[Authorize]
[ApiController]
[Produces("application/json")]
public class CategoriesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Gets a list of all available categories. OData filtering is supported, e.g. $filter=name eq 'march'.
    /// </summary>
    /// <param name="queryParams">The OData query parameters specified</param>
    /// <response code="200">A list of categories matching filter, or all categories. Empty list if no matching results</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    [HttpGet("categories")]
    public async Task<ActionResult<List<ApiCategory>>> GetCategoryList([FromQuery] ODataQueryParams queryParams)
    {
        var results = await mediator.Send(new GetCategoryCollection(queryParams));

        return results.Select(c => new ApiCategory(c)).ToList();
    }

    /// <summary>
    /// Gets details about the category identified by <paramref name="categoryIdentifier"/>.
    /// </summary>
    /// <param name="categoryIdentifier">A value uniquely identifying the category. Either guid or name</param>
    /// <response code="200">The category matching the identifier</response>
    /// <response code="404">Category not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    [HttpGet("categories/{categoryIdentifier}")]
    public async Task<ActionResult<ApiCategory>> GetCategory(string categoryIdentifier)
    {
        var category = await mediator.Send(new GetCategory(categoryIdentifier));

        if (category is null)
            return NotFound(new ProblemDetails { Detail = $"Category '{categoryIdentifier}' was not found" });

        return new ApiCategory(category);
    }

    /// <summary>
    /// Adds a new category.
    /// Requires Administrator privileges.
    /// </summary>
    /// <param name="request">Details about the new category</param>
    /// <response code="200">Details about the newly created category</response>
    /// <response code="400">If provided input is invalid. Should include a body with ProblemDetails-formatted errors.</response>
    /// <response code="409">A category with the same name already exists</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpPost("categories")]
    public async Task<ActionResult<ApiCategory>> AddNewCategory(CategoryRequest request)
    {
        var category = await mediator.Send(new AddCategory(request.Name, request.Inactive ?? false));

        return new ApiCategory(category);
    }

    /// <summary>
    /// Updates the category identified by <paramref name="categoryIdentifier"/>.
    /// Requires Administrator privileges.
    /// </summary>
    /// <param name="categoryIdentifier">A value uniquely identifying the category. Either guid or name</param>
    /// <param name="request">Updated details for the category</param>
    /// <response code="200">The updated category</response>
    /// <response code="404">Category not found</response>
    /// <response code="409">A different category with the same name already exists</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpPut("categories/{categoryIdentifier}")]
    public async Task<ActionResult<ApiCategory>> UpdateCategory(string categoryIdentifier, CategoryRequest request)
    {
        var category = await mediator.Send(new GetCategory(categoryIdentifier));

        if (category is null)
            return NotFound(new ProblemDetails { Detail = $"Category '{categoryIdentifier}' was not found" });

        await mediator.Send(new UpdateCategory(category.Id, request));

        category = await mediator.Send(new GetCategory(category.Id.ToString()));

        if (category is null)
            throw new NotFoundError(categoryIdentifier, "Category was not found");

        return new ApiCategory(category);
    }

    /// <summary>
    /// Deletes the category identified by <paramref name="categoryIdentifier"/>.
    /// Requires Administrator privileges.
    /// </summary>
    /// <param name="categoryIdentifier">A value uniquely identifying the category. Either guid or name</param>
    /// <response code="204">Category was deleted successfully</response>
    /// <response code="404">Category not found</response>
    /// <response code="409">Category is assigned to one or more sets and cannot be deleted</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpDelete("categories/{categoryIdentifier}")]
    public async Task<ActionResult> DeleteCategory(string categoryIdentifier)
    {
        var category = await mediator.Send(new GetCategory(categoryIdentifier));

        if (category is null)
            return NotFound(new ProblemDetails { Detail = $"Category '{categoryIdentifier}' was not found" });

        await mediator.Send(new DeleteCategory(category.Id));

        return NoContent();
    }
}

