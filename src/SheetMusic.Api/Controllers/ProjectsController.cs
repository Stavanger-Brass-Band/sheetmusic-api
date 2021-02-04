using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheetMusic.Api.Authorization;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.Controllers.ViewModels;
using SheetMusic.Api.CQRS.Command;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using SheetMusic.Api.OData.MVC;
using SheetMusic.Api.Repositories;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectRepository projectRepository;
        private readonly IMediator mediator;

        public ProjectsController(IProjectRepository projectRepository, IMediator mediator)
        {
            this.projectRepository = projectRepository;
            this.mediator = mediator;
        }

        [HttpGet("projects")]
        public async Task<IActionResult> GetProjects([FromQuery] ODataQueryParams? query)
        {
            var projects = await projectRepository.GetProjectsAsync(query);

            return new OkObjectResult(projects.Select(p => new ApiProject(p)));
        }

        [HttpGet("projects/{projectIdentifier}")]
        public async Task<IActionResult> GetProject(string projectIdentifier)
        {
            var project = await projectRepository.ResolveByIdentifierAsync(projectIdentifier);

            return new OkObjectResult(new ApiProject(project));
        }

        [HttpGet("projects/{projectIdentifier}/sets")]

        public async Task<IActionResult> GetSetsForProject(string projectIdentifier)
        {
            var project = await projectRepository.ResolveByIdentifierAsync(projectIdentifier);
            var sets = await projectRepository.GetSetsForProjectAsync(project.Id);

            return new OkObjectResult(sets.Select(s => new ApiSet(s)));
        }

        [Authorize(AuthPolicy.Admin)]
        [HttpPost("projects")]
        public async Task<IActionResult> CreateNewProject([FromBody] NewProjectRequest request)
        {
            var project = new Project
            {
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            project = await projectRepository.AddNewProjectAsync(project);

            return new OkObjectResult(new ApiProject(project));
        }

        [Authorize(AuthPolicy.Admin)]
        [HttpPost("projects/{projectIdentifier}/sets")]
        public async Task<IActionResult> AssignSetToProject(string projectIdentifier, [FromBody] SetCollectionRequest request)
        {
            var project = await projectRepository.ResolveByIdentifierAsync(projectIdentifier);

            foreach (var setId in request.SetIdentifiers)
            {
                var set = await mediator.Send(new GetSet(setId));

                if (set is null) continue;

                await projectRepository.ConnectSetWithProjectAsync(project.Id, set.Id);
            }

            var setsForProject = await projectRepository.GetSetsForProjectAsync(project.Id);

            return new CreatedResult($"projects/{projectIdentifier}/sets", setsForProject.Select(s => new ApiSet(s)));
        }

        [Authorize(AuthPolicy.Admin)]
        [HttpDelete("projects/{projectIdentifier}/sets/")]
        public async Task<IActionResult> UnassignSetFromProject(string projectIdentifier, [FromBody] SetCollectionRequest request)
        {
            var project = await projectRepository.ResolveByIdentifierAsync(projectIdentifier);

            foreach (var setId in request.SetIdentifiers)
            {
                var set = await mediator.Send(new GetSet(setId));

                if (set is null) continue;

                await projectRepository.DisconnectSetFromProjectAsync(project.Id, set.Id);
            }

            var setsForProject = await projectRepository.GetSetsForProjectAsync(project.Id);

            return new OkObjectResult(setsForProject.Select(s => new ApiSet(s)));
        }

        [Authorize(AuthPolicy.Admin)]
        [HttpDelete("projects/{projectIdentifier}")]
        public async Task<ActionResult> DeleteProject(string projectIdentifier)
        {
            try
            {
                await projectRepository.DeleteProjectAsync(projectIdentifier);
            }
            catch (NotFoundError)
            {
                return NotFound();
            }

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
}
