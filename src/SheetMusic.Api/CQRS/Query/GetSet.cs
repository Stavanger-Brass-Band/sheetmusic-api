﻿using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Query;

public class GetSet(string setIdentifier) : IRequest<SheetMusicSet?>
{
    public string SetIdentifier { get; } = setIdentifier;

    public class Handler(SheetMusicContext db) : IRequestHandler<GetSet, SheetMusicSet?>
    {
        public async Task<SheetMusicSet?> Handle(GetSet request, CancellationToken cancellationToken)
        {
            SheetMusicSet? result = null;

            if (Guid.TryParse(request.SetIdentifier, out var guid))
            {
                result = await db.SheetMusicSets
                    .Include(s => s.Parts).ThenInclude(p => p.Part)
                    .FirstOrDefaultAsync(set => set.Id == guid, cancellationToken: cancellationToken);
            }
            else if (int.TryParse(request.SetIdentifier, out var archiveNumber))
            {
                result = await db.SheetMusicSets
                    .Include(s => s.Parts).ThenInclude(p => p.Part)
                    .FirstOrDefaultAsync(set => set.ArchiveNumber == archiveNumber, cancellationToken: cancellationToken);
            }
            else
            {
                //ignore casing when comparing on title
                result = await db.SheetMusicSets
                    .Include(s => s.Parts).ThenInclude(p => p.Part)
                    .SingleOrDefaultAsync(set => set.Title.ToLower() == request.SetIdentifier.ToLower(), cancellationToken: cancellationToken);
            }
            
            return result;
        }
    }
}
