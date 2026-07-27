using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Projects;

public class ConnectSetToProject(Guid projectId, Guid setId) : IRequest
{
    public Guid ProjectId { get; } = projectId;
    public Guid SetId { get; } = setId;

    public class Handler(SheetMusicContext db) : IRequestHandler<ConnectSetToProject>
    {
        public async Task Handle(ConnectSetToProject request, CancellationToken cancellationToken)
        {
            var existingSortOrders = await db.ProjectSheetMusicSets
                .Where(c => c.ProjectId == request.ProjectId)
                .Select(c => (int?)c.SortOrder)
                .ToListAsync(cancellationToken);

            var nextSortOrder = existingSortOrders.Count > 0 ? existingSortOrders.Max()!.Value + 1 : 0;

            var connection = new ProjectSheetMusicSet
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                SheetMusicSetId = request.SetId,
                SortOrder = nextSortOrder
            };

            await db.ProjectSheetMusicSets.AddAsync(connection, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
