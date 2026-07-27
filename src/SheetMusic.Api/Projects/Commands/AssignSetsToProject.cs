using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Projects.Commands;

/// <summary>
/// Connects the given sets to a project, appending any that are not yet assigned. The order of
/// <paramref name="setIds"/> determines the sort order of those sets - sets already assigned to the
/// project are moved to match their position in the list, so this command also covers reordering.
/// </summary>
public class AssignSetsToProject(Guid projectId, IReadOnlyList<Guid> setIds) : IRequest
{
    public Guid ProjectId { get; } = projectId;
    public IReadOnlyList<Guid> SetIds { get; } = setIds;

    public class Handler(SheetMusicContext db) : IRequestHandler<AssignSetsToProject>
    {
        public async Task Handle(AssignSetsToProject request, CancellationToken cancellationToken)
        {
            var connections = await db.ProjectSheetMusicSets
                .Where(c => c.ProjectId == request.ProjectId)
                .ToListAsync(cancellationToken);

            var connectionsBySetId = connections.ToDictionary(c => c.SheetMusicSetId);
            var nextSortOrder = connections.Count > 0 ? connections.Max(c => c.SortOrder) + 1 : 0;

            for (var index = 0; index < request.SetIds.Count; index++)
            {
                var setId = request.SetIds[index];

                if (connectionsBySetId.TryGetValue(setId, out var existingConnection))
                {
                    existingConnection.SortOrder = index;
                    continue;
                }

                await db.ProjectSheetMusicSets.AddAsync(new ProjectSheetMusicSet
                {
                    Id = Guid.NewGuid(),
                    ProjectId = request.ProjectId,
                    SheetMusicSetId = setId,
                    SortOrder = nextSortOrder++
                }, cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
