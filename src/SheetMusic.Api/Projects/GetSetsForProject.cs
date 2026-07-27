using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Projects;

public class GetSetsForProject(Guid projectId) : IRequest<List<SheetMusicSet>>
{
    public Guid ProjectId { get; } = projectId;

    public class Handler(SheetMusicContext db) : IRequestHandler<GetSetsForProject, List<SheetMusicSet>>
    {
        public async Task<List<SheetMusicSet>> Handle(GetSetsForProject request, CancellationToken cancellationToken)
        {
            var query = from project in db.Projects
                        from setConnection in project.SetConnections
                        where project.Id == request.ProjectId
                        orderby setConnection.SortOrder
                        select setConnection.Set;

            return await query.ToListAsync(cancellationToken);
        }
    }
}
