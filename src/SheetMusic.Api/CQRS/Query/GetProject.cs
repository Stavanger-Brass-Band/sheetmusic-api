using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Query
{
    public class GetProject : IRequest<Project>
    {
        private readonly string identifier;

        public GetProject(string identifier)
        {
            this.identifier = identifier;
        }

        public class Handler : IRequestHandler<GetProject, Project>
        {
            private readonly SheetMusicContext db;

            public Handler(SheetMusicContext db)
            {
                this.db = db;
            }

            public async Task<Project> Handle(GetProject request, CancellationToken cancellationToken)
            {
                Project result;

                if (Guid.TryParse(request.identifier, out var guid))
                {
                    result = await db.Projects.FirstOrDefaultAsync(project => project.Id == guid);
                }
                else
                {
                    //ignore casing when comparing on title
                    result = await db.Projects.SingleOrDefaultAsync(project => project.Name.ToLower() == request.identifier.ToLower());
                }

                if (result == null) throw new NotFoundError($"projects/{request.identifier}", "Project not found");

                return result;
            }
        }
    }
}
