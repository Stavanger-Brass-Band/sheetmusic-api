using MediatR;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command
{
    public class AddPart : IRequest
    {
        public AddPart(string name, int sortOrder, bool indexable)
        {
            Name = name;
            SortOrder = sortOrder;
            Indexable = indexable;
        }

        public string Name { get; }
        public int SortOrder { get; }
        public bool Indexable { get; }

        public class Handler : AsyncRequestHandler<AddPart>
        {
            private readonly SheetMusicContext db;

            public Handler(SheetMusicContext db)
            {
                this.db = db;
            }

            protected override async Task Handle(AddPart request, CancellationToken cancellationToken)
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
            }
        }
    }
}
