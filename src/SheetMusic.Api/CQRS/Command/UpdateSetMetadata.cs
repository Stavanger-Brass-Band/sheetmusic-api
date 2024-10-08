﻿using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class UpdateSetMetadata(Guid setId, SetRequest setMetadta) : IRequest
{
    public Guid SetId { get; } = setId;
    public SetRequest SetMetadata { get; } = setMetadta;

    public class Handler(SheetMusicContext db) : IRequestHandler<UpdateSetMetadata>
    {
        public async Task Handle(UpdateSetMetadata request, CancellationToken cancellationToken)
        {
            var set = await db.SheetMusicSets.FirstOrDefaultAsync(s => s.Id == request.SetId);

            if (set == null) throw new NotFoundError(request.SetId.ToString(), "Set not found");

            if (request.SetMetadata.ArchiveNumber.HasValue)
                set.ArchiveNumber = request.SetMetadata.ArchiveNumber.Value;

            set.Title = request.SetMetadata.Title;
            set.Composer = request.SetMetadata.Composer;
            set.Arranger = request.SetMetadata.Arranger;
            set.SoleSellingAgent = request.SetMetadata.SoleSellingAgent;
            set.MissingParts = request.SetMetadata.MissingParts;
            set.RecordingUrl = request.SetMetadata.RecordingUrl;

            if (set.BorrowedFrom != request.SetMetadata.BorrowedFrom)
            {
                set.BorrowedFrom = request.SetMetadata.BorrowedFrom;
                set.BorrowedDateTime = DateTimeOffset.Now;
            }

            await db.SaveChangesAsync();
        }
    }
}
