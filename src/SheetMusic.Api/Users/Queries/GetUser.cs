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

namespace SheetMusic.Api.Users.Queries;

public class GetUser(Guid userId) : IRequest<GetUser.Result>
{
    public Guid UserId { get; } = userId;

    public record Result(ApplicationUser User, IReadOnlyList<string> Roles, IReadOnlyList<MusicPart> Parts);

    public class Handler(UserManager<ApplicationUser> userManager, SheetMusicContext db) : IRequestHandler<GetUser, Result>
    {
        public async Task<Result> Handle(GetUser request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(request.UserId.ToString())
                ?? throw new NotFoundError($"users/{request.UserId}", "User not found");

            var roles = await userManager.GetRolesAsync(user);
            var parts = await db.Musicians
                .Where(musician => musician.ApplicationUserId == request.UserId)
                .SelectMany(musician => musician.MusicianMusicParts)
                .Include(musicianPart => musicianPart.MusicPart)
                .ThenInclude(part => part.Aliases)
                .OrderBy(musicianPart => musicianPart.MusicPart.SortOrder)
                .Select(musicianPart => musicianPart.MusicPart)
                .ToListAsync(cancellationToken);

            return new Result(user, roles.ToList(), parts);
        }
    }
}
