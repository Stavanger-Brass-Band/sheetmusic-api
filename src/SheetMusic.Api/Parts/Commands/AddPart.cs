using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Parts.Errors;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Parts.Commands;

public class AddPart(string name, int sortOrder, bool indexable, bool alwaysDisplay, InstrumentGroup? instrumentGroup) : IRequest<MusicPart>
{
    public string Name { get; } = name;
    public int SortOrder { get; } = sortOrder;
    public bool Indexable { get; } = indexable;
    public bool AlwaysDisplay { get; } = alwaysDisplay;
    public InstrumentGroup? InstrumentGroup { get; } = instrumentGroup;

    public class Handler(SheetMusicContext db) : IRequestHandler<AddPart, MusicPart>
    {
        public async Task<MusicPart> Handle(AddPart request, CancellationToken cancellationToken)
        {
            if (await db.MusicParts.AnyAsync(p => p.Name.ToLower() == request.Name.ToLower(), cancellationToken))
                throw new PartAlreadyExistsError(request.Name);

            var part = new MusicPart
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Indexable = request.Indexable,
                AlwaysDisplay = request.AlwaysDisplay,
                SortOrder = request.SortOrder,
                InstrumentGroup = request.InstrumentGroup
            };

            db.MusicParts.Add(part);
            await db.SaveChangesAsync(cancellationToken);

            return part;
        }
    }
}
