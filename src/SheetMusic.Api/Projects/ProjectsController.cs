using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheetMusic.Api.Authorization;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.Controllers.ViewModels;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.OData.MVC;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.Projects;

[Authorize]
[ApiController]
public class ProjectsController(IMediator mediator) : ControllerBase
{
    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects([FromQuery] ODataQueryParams? query)
    {
        var projects = await mediator.Send(new GetProjectCollection(query));

        return new OkObjectResult(projects.Select(p => new ApiProject(p)));
    }

    [HttpGet("projects/{projectIdentifier}")]
    public async Task<IActionResult> GetProject(string projectIdentifier)
    {
        var project = await mediator.Send(new GetProject(projectIdentifier));

        return new OkObjectResult(new ApiProject(project));
    }

    [HttpGet("projects/{projectIdentifier}/sets")]

    public async Task<IActionResult> GetSetsForProject(string projectIdentifier)
    {
        var project = await mediator.Send(new GetProject(projectIdentifier));
        var sets = await mediator.Send(new GetSetsForProject(project.Id));

        return new OkObjectResult(sets.Select(s => new ApiSet(s)));
    }

    [Authorize(AuthPolicy.Admin)]
    [HttpPost("projects")]
    public async Task<IActionResult> CreateNewProject([FromBody] NewProjectRequest request)
    {
        var project = await mediator.Send(new AddProject(request));

        return new OkObjectResult(new ApiProject(project));
    }

    [Authorize(AuthPolicy.Admin)]
    [HttpPost("projects/{projectIdentifier}/sets")]
    public async Task<IActionResult> AssignSetToProject(string projectIdentifier, [FromBody] SetCollectionRequest request)
    {
        var project = await mediator.Send(new GetProject(projectIdentifier));

        foreach (var setId in request.SetIdentifiers)
        {
            var set = await mediator.Send(new GetSet(setId));

            if (set is null) continue;

            await mediator.Send(new ConnectSetToProject(project.Id, set.Id));
        }

        var setsForProject = await mediator.Send(new GetSetsForProject(project.Id));

        return new CreatedResult($"projects/{projectIdentifier}/sets", setsForProject.Select(s => new ApiSet(s)));
    }

    [Authorize(AuthPolicy.Admin)]
    [HttpPut("projects/{projectIdentifier}/sets/order")]
    public async Task<IActionResult> UpdateSetOrderForProject(string projectIdentifier, [FromBody] SetCollectionRequest request)
    {
        var project = await mediator.Send(new GetProject(projectIdentifier));

        await mediator.Send(new UpdateSetOrderForProject(project.Id, request));

        var setsForProject = await mediator.Send(new GetSetsForProject(project.Id));

        return new OkObjectResult(setsForProject.Select(s => new ApiSet(s)));
    }

    [Authorize(AuthPolicy.Admin)]
    [HttpDelete("projects/{projectIdentifier}/sets/")]
    public async Task<IActionResult> UnassignSetFromProject(string projectIdentifier, [FromBody] SetCollectionRequest request)
    {
        var project = await mediator.Send(new GetProject(projectIdentifier));

        foreach (var setId in request.SetIdentifiers)
        {
            var set = await mediator.Send(new GetSet(setId));

            if (set is null) continue;

            await mediator.Send(new DisconnectSetFromProject(project.Id, set.Id));
        }

        var setsForProject = await mediator.Send(new GetSetsForProject(project.Id));

        return new OkObjectResult(setsForProject.Select(s => new ApiSet(s)));
    }

    [Authorize(AuthPolicy.Admin)]
    [HttpDelete("projects/{projectIdentifier}")]
    public async Task<ActionResult> DeleteProject(string projectIdentifier)
    {
        await mediator.Send(new DeleteProject(projectIdentifier));

        return NoContent();
    }

    [Authorize(AuthPolicy.Admin)]
    [HttpPut("projects/{projectIdentifier}")]
    public async Task<IActionResult> UpdateProject(string projectIdentifier, UpdateProjectRequest request)
    {
        await mediator.Send(new UpdateProjectMetadata(projectIdentifier, request));
        var project = await mediator.Send(new GetProject(projectIdentifier));

        return new OkObjectResult(new ApiProject(project));
    }
}
