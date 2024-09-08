using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class AddSet(SetRequest input) : IRequest
{
    public SetRequest Input { get; } = input;

    public class Handler(SheetMusicContext db) : IRequestHandler<AddSet>
    {
        public async Task Handle(AddSet request, CancellationToken cancellationToken)
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
