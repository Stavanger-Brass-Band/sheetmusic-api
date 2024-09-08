using MediatR;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class AddPart(string name, int sortOrder, bool indexable) : IRequest<MusicPart>
{
    public string Name { get; } = name;
    public int SortOrder { get; } = sortOrder;
    public bool Indexable { get; } = indexable;

    public class Handler(SheetMusicContext db) : IRequestHandler<AddPart, MusicPart>
    {
        public async Task<MusicPart> Handle(AddPart request, CancellationToken cancellationToken)
        {
            if (db.MusicParts.Any(p => p.Name.ToLower() == request.Name.ToLower()))
                throw new PartAlreadyExistsError(request.Name);

            var part = new MusicPart
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Indexable = request.Indexable,
                SortOrder = request.SortOrder
            };

            db.MusicParts.Add(part);
            await db.SaveChangesAsync(cancellationToken);

            return part;
        }
    }
}
