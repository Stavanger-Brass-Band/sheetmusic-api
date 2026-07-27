using MediatR;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class ConnectSetToProject(Guid projectId, Guid setId) : IRequest
{
    public Guid ProjectId { get; } = projectId;
    public Guid SetId { get; } = setId;

    public class Handler(SheetMusicContext db) : IRequestHandler<ConnectSetToProject>
    {
        public async Task Handle(ConnectSetToProject request, CancellationToken cancellationToken)
        {
            var connection = new ProjectSheetMusicSet
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                SheetMusicSetId = request.SetId
            };

            await db.ProjectSheetMusicSets.AddAsync(connection, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
