using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class AddAliasToPart : IRequest
{
    public AddAliasToPart(Guid partId, string alias)
    {
        PartId = partId;
        Alias = alias;
    }

    public Guid PartId { get; }
    public string Alias { get; }

    public class Handler : AsyncRequestHandler<AddAliasToPart>
    {
        private readonly SheetMusicContext db;

        public Handler(SheetMusicContext db)
        {
            this.db = db;
        }

        protected override async Task Handle(AddAliasToPart request, CancellationToken cancellationToken)
        {
            var part = await db.MusicParts
                .Include(p => p.Aliases)
                .FirstOrDefaultAsync(p => p.Id == request.PartId, cancellationToken: cancellationToken);

            if (part?.Aliases.Any(a => a.Alias.ToLower() == request.Alias.ToLower()) ?? false)
                throw new AliasAlreadyAddedError(request.Alias, part.Name);

            var alias = new MusicPartAlias
            {
                Id = Guid.NewGuid(),
                Alias = request.Alias,
                Enabled = true,
                MusicPartId = request.PartId
            };

            db.MusicPartAliases.Add(alias);
            
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
