using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheetMusic.Api.Controllers.ViewModels;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.OData.MVC;
using SheetMusic.Api.Projects.Commands;
using SheetMusic.Api.Projects.Queries;
using SheetMusic.Api.Projects.RequestModels;
using SheetMusic.Api.Projects.ViewModels;
using System;
using System.Collections.Generic;
using SheetMusic.Api.Users.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.Projects;

/// <summary>
/// This controller contains endpoints for managing Project resources.
/// Projects group sheet music sets together, e.g. for a concert or event.
///
/// PS! Creating, updating and deleting projects require Administrator privileges.
/// </summary>
[Authorize]
[ApiController]
public class ProjectsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Gets a list of all projects. OData filtering is supported, e.g. $filter=name eq 'Christmas concert'.
    /// </summary>
    /// <param name="query">The OData query parameters specified</param>
    /// <response code="200">A list of projects matching filter, or all projects. Empty list if no matching results</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    [HttpGet("projects")]
    public async Task<ActionResult<List<ApiProject>>> GetProjects([FromQuery] ODataQueryParams? query)
    {
        var projects = await mediator.Send(new GetProjectCollection(query));

        return projects.Select(p => new ApiProject(p)).ToList();
    }

    /// <summary>
    /// Gets details about the project identified by <paramref name="projectIdentifier"/>.
    /// </summary>
    /// <param name="projectIdentifier">A value uniquely identifying the project. Either guid or name</param>
    /// <response code="200">The project matching the identifier</response>
    /// <response code="404">Project not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    [HttpGet("projects/{projectIdentifier}")]
    public async Task<ActionResult<ApiProject>> GetProject(string projectIdentifier)
    {
        var project = await mediator.Send(new GetProject(projectIdentifier));

        return new ApiProject(project);
    }

    /// <summary>
    /// Gets the sets assigned to the project identified by <paramref name="projectIdentifier"/>.
    /// </summary>
    /// <param name="projectIdentifier">A value uniquely identifying the project. Either guid or name</param>
    /// <response code="200">List of sets assigned to the project</response>
    /// <response code="404">Project not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    [HttpGet("projects/{projectIdentifier}/sets")]
    public async Task<ActionResult<List<ApiSet>>> GetSetsForProject(string projectIdentifier)
    {
        var project = await mediator.Send(new GetProject(projectIdentifier));
        var sets = await mediator.Send(new GetSetsForProject(project.Id));

        return sets.Select(s => new ApiSet(s)).ToList();
    }

    /// <summary>
    /// Adds a new project.
    /// Requires Administrator privileges.
    /// </summary>
    /// <param name="request">Details about the new project</param>
    /// <response code="200">Details about the newly created project</response>
    /// <response code="400">If provided input is invalid. Should include a body with ProblemDetails-formatted errors.</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpPost("projects")]
    public async Task<ActionResult<ApiProject>> CreateNewProject([FromBody] NewProjectRequest request)
    {
        var project = await mediator.Send(new AddProject(request));

        return new ApiProject(project);
    }

    /// <summary>
    /// Assigns the given sets to a project. The order of <see cref="SetCollectionRequest.SetIdentifiers"/> determines
    /// the sort order of those sets - sets already assigned to the project are moved to match their position in the
    /// list, so this endpoint also covers reordering the sets currently assigned to a project.
    /// Requires Administrator privileges.
    /// </summary>
    /// <param name="projectIdentifier">A value uniquely identifying the project. Either guid or name</param>
    /// <param name="request">The identifiers (guid, archive number or title) of the sets to assign, in the desired order</param>
    /// <response code="201">The updated list of sets assigned to the project</response>
    /// <response code="400">If provided input is invalid. Should include a body with ProblemDetails-formatted errors.</response>
    /// <response code="404">Project not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpPost("projects/{projectIdentifier}/sets")]
    public async Task<ActionResult<List<ApiSet>>> AssignSetToProject(string projectIdentifier, [FromBody] SetCollectionRequest request)
    {
        var project = await mediator.Send(new GetProject(projectIdentifier));

        var setIds = new List<Guid>();

        foreach (var setIdentifier in request.SetIdentifiers)
        {
            var set = await mediator.Send(new GetSet(setIdentifier));

            if (set is null) continue;

            setIds.Add(set.Id);
        }

        await mediator.Send(new AssignSetsToProject(project.Id, setIds));

        var setsForProject = await mediator.Send(new GetSetsForProject(project.Id));

        return new CreatedResult($"projects/{projectIdentifier}/sets", setsForProject.Select(s => new ApiSet(s)));
    }

    /// <summary>
    /// Removes the given sets from a project.
    /// Requires Administrator privileges.
    /// </summary>
    /// <param name="projectIdentifier">A value uniquely identifying the project. Either guid or name</param>
    /// <param name="request">The identifiers (guid, archive number or title) of the sets to unassign</param>
    /// <response code="200">The updated list of sets assigned to the project</response>
    /// <response code="400">If provided input is invalid. Should include a body with ProblemDetails-formatted errors.</response>
    /// <response code="404">Project not found, or one of the sets is not assigned to the project</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpDelete("projects/{projectIdentifier}/sets/")]
    public async Task<ActionResult<List<ApiSet>>> UnassignSetFromProject(string projectIdentifier, [FromBody] SetCollectionRequest request)
    {
        var project = await mediator.Send(new GetProject(projectIdentifier));

        foreach (var setId in request.SetIdentifiers)
        {
            var set = await mediator.Send(new GetSet(setId));

            if (set is null) continue;

            await mediator.Send(new DisconnectSetFromProject(project.Id, set.Id));
        }

        var setsForProject = await mediator.Send(new GetSetsForProject(project.Id));

        return setsForProject.Select(s => new ApiSet(s)).ToList();
    }

    /// <summary>
    /// Deletes the project identified by <paramref name="projectIdentifier"/>.
    /// </summary>
    /// <param name="projectIdentifier">A value uniquely identifying the project. Either guid or name</param>
    /// <response code="204">Project was deleted successfully</response>
    /// <response code="404">Project not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpDelete("projects/{projectIdentifier}")]
    public async Task<ActionResult> DeleteProject(string projectIdentifier)
    {
        await mediator.Send(new DeleteProject(projectIdentifier));

        return NoContent();
    }

    /// <summary>
    /// Updates the project identified by <paramref name="projectIdentifier"/>. PS! Provide all values, those not provided will be set to null.
    /// Requires Administrator privileges.
    /// </summary>
    /// <param name="projectIdentifier">A value uniquely identifying the project. Either guid or name</param>
    /// <param name="request">Updated details for the project</param>
    /// <response code="200">The updated project</response>
    /// <response code="400">If provided input is invalid. Should include a body with ProblemDetails-formatted errors.</response>
    /// <response code="404">Project not found</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpPut("projects/{projectIdentifier}")]
    public async Task<ActionResult<ApiProject>> UpdateProject(string projectIdentifier, UpdateProjectRequest request)
    {
        await mediator.Send(new UpdateProjectMetadata(projectIdentifier, request));
        var project = await mediator.Send(new GetProject(projectIdentifier));

        return new ApiProject(project);
    }
}
