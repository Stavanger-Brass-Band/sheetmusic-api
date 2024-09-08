using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class RemoveAliasFromPart(Guid partId, string aliasToRemove) : IRequest
{
    public Guid PartId { get; } = partId;
    public string AliasToRemove { get; } = aliasToRemove;

    public class Handler(SheetMusicContext db) : IRequestHandler<RemoveAliasFromPart>
    {
        public async Task Handle(RemoveAliasFromPart request, CancellationToken cancellationToken)
        {
            var part = await db.MusicParts
                .Include(p => p.Aliases)
                .FirstOrDefaultAsync(p => p.Id == request.PartId, cancellationToken: cancellationToken);

            if (part == null) throw new NotFoundError(request.PartId.ToString(), "Part not found");

            var alias = part.Aliases.FirstOrDefault(a => a.Alias.ToLower() == request.AliasToRemove.ToLower());

            if (alias is null) throw new NotFoundError(request.AliasToRemove, "Alias not found");

            db.MusicPartAliases.Remove(alias);

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
