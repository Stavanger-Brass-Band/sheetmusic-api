using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Projects;

/// <summary>
/// Updates the sort order of the sets assigned to a project. The order of <see cref="SetCollectionRequest.SetIdentifiers"/>
/// determines the new sort order, and must contain exactly the set identifiers currently assigned to the project.
/// </summary>
public class UpdateSetOrderForProject(Guid projectId, SetCollectionRequest request) : IRequest
{
    public Guid ProjectId { get; } = projectId;
    public SetCollectionRequest Request { get; } = request;

    public class Handler(SheetMusicContext db) : IRequestHandler<UpdateSetOrderForProject>
    {
        public async Task Handle(UpdateSetOrderForProject command, CancellationToken cancellationToken)
        {
            var connections = await db.ProjectSheetMusicSets
                .Where(c => c.ProjectId == command.ProjectId)
                .ToListAsync(cancellationToken);

            var connectionsBySetId = connections.ToDictionary(c => c.SheetMusicSetId);

            var parsedSetIds = new List<Guid>();

            foreach (var identifier in command.Request.SetIdentifiers)
            {
                if (!Guid.TryParse(identifier, out var setId) || !connectionsBySetId.ContainsKey(setId))
                    throw new InvalidSetOrderError(command.ProjectId);

                parsedSetIds.Add(setId);
            }

            if (parsedSetIds.Count != connections.Count || parsedSetIds.Distinct().Count() != connections.Count)
                throw new InvalidSetOrderError(command.ProjectId);

            for (var index = 0; index < parsedSetIds.Count; index++)
            {
                connectionsBySetId[parsedSetIds[index]].SortOrder = index;
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
