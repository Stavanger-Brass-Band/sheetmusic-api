using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command
{
    public class AddSet : IRequest
    {
        public AddSet(SetRequest input)
        {
            Input = input;
        }

        public SetRequest Input { get; }

        public class Handler : AsyncRequestHandler<AddSet>
        {
            private readonly SheetMusicContext db;

            public Handler(SheetMusicContext db)
            {
                this.db = db;
            }

            protected override async Task Handle(AddSet request, CancellationToken cancellationToken)
            {
                var input = request.Input;
                int? archiveNumber = input.ArchiveNumber;

                if (archiveNumber is null)
                {
                    archiveNumber = await db.SheetMusicSets.AnyAsync(cancellationToken) ?
                        await db.SheetMusicSets.MaxAsync(s => s.ArchiveNumber, cancellationToken) + 1 : 1;
                }

                if (await db.SheetMusicSets.AnyAsync(s => s.ArchiveNumber == archiveNumber, cancellationToken))
                    throw new ArchiveNumberOccupiedError(archiveNumber.Value);

                var set = new SheetMusicSet(archiveNumber.Value, input.Title)
                {
                    Composer = input.Composer,
                    Arranger = input.Arranger,
                    SoleSellingAgent = input.SoleSellingAgent,
                    MissingParts = input.MissingParts,
                    BorrowedFrom = input.BorrowedFrom,
                    BorrowedDateTime = string.IsNullOrEmpty(input.BorrowedFrom) ? null : (DateTimeOffset?)DateTimeOffset.Now
                };

                await db.SheetMusicSets.AddAsync(set, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
