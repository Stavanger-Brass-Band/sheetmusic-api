using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Projects;

public class DisconnectSetFromProject(Guid projectId, Guid setId) : IRequest
{
    public Guid ProjectId { get; } = projectId;
    public Guid SetId { get; } = setId;

    public class Handler(SheetMusicContext db) : IRequestHandler<DisconnectSetFromProject>
    {
        public async Task Handle(DisconnectSetFromProject request, CancellationToken cancellationToken)
        {
            var connection = await db.ProjectSheetMusicSets.FirstOrDefaultAsync(
                ps => ps.ProjectId == request.ProjectId && ps.SheetMusicSetId == request.SetId,
                cancellationToken);

            if (connection is null)
                throw new NotFoundError($"projects/{request.ProjectId}/sets/{request.SetId}", "Connection does not exist");

            db.ProjectSheetMusicSets.Remove(connection);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
