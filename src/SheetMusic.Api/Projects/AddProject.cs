using MediatR;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Projects;

public class AddProject(NewProjectRequest request) : IRequest<Project>
{
    public NewProjectRequest Request { get; } = request;

    public class Handler(SheetMusicContext db) : IRequestHandler<AddProject, Project>
    {
        public async Task<Project> Handle(AddProject command, CancellationToken cancellationToken)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = command.Request.Name,
                StartDate = command.Request.StartDate,
                EndDate = command.Request.EndDate,
                Comments = command.Request.Comments
            };

            await db.Projects.AddAsync(project, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            return project;
        }
    }
}
