using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users.Commands;

public class AssignPartsToUser(Guid userId, IReadOnlyList<Guid> partIds) : IRequest
{
    public Guid UserId { get; } = userId;
    public IReadOnlyList<Guid> PartIds { get; } = partIds;

    public class Handler(SheetMusicContext db, UserManager<ApplicationUser> userManager) : IRequestHandler<AssignPartsToUser>
    {
        public async Task Handle(AssignPartsToUser request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(request.UserId.ToString())
                ?? throw new NotFoundError($"users/{request.UserId}", "User not found");

            var musician = await db.Musicians
                .SingleOrDefaultAsync(m => m.ApplicationUserId == request.UserId, cancellationToken);

            if (musician is null)
            {
                musician = new Musician
                {
                    Id = Guid.NewGuid(),
                    Name = user.DisplayName,
                    ApplicationUserId = user.Id
                };
                await db.Musicians.AddAsync(musician, cancellationToken);
            }

            var existingPartIds = await db.MusicParts
                .Where(part => request.PartIds.Contains(part.Id))
                .Select(part => part.Id)
                .ToListAsync(cancellationToken);

            if (existingPartIds.Count != request.PartIds.Count)
                throw new NotFoundError("parts", "One or more parts were not found");

            var existingAssignments = await db.Set<MusicianMusicPart>()
                .Where(assignment => assignment.MusicianId == musician.Id)
                .ToListAsync(cancellationToken);
            db.RemoveRange(existingAssignments);

            foreach (var partId in request.PartIds)
            {
                await db.Set<MusicianMusicPart>().AddAsync(new MusicianMusicPart
                {
                    Id = Guid.NewGuid(),
                    MusicianId = musician.Id,
                    MusicPartId = partId
                }, cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}