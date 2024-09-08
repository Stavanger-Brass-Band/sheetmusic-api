using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Query;

public class GetProject(string identifier) : IRequest<Project>
{
    private readonly string identifier = identifier;

    public class Handler(SheetMusicContext db) : IRequestHandler<GetProject, Project>
    {
        public async Task<Project> Handle(GetProject request, CancellationToken cancellationToken)
        {
            Project? result;

            if (Guid.TryParse(request.identifier, out var guid))
            {
                result = await db.Projects.FirstOrDefaultAsync(project => project.Id == guid, cancellationToken: cancellationToken);
            }
            else
            {
                //ignore casing when comparing on title
                result = await db.Projects.SingleOrDefaultAsync(project => project.Name.ToLower() == request.identifier.ToLower(), cancellationToken: cancellationToken);
            }

            if (result is null)
                throw new NotFoundError($"projects/{request.identifier}", "Project not found");

            return result;
        }
    }
}
