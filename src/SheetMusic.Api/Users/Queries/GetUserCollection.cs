using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users.Queries;

public class GetUserCollection : IRequest<IReadOnlyList<GetUserCollection.Result>>
{
    public record Result(ApplicationUser User, IReadOnlyList<string> Roles, IReadOnlyList<MusicPart> Parts);

    public class Handler(SheetMusicContext db) : IRequestHandler<GetUserCollection, IReadOnlyList<Result>>
    {
        public async Task<IReadOnlyList<Result>> Handle(GetUserCollection request, CancellationToken cancellationToken)
        {
            return await db.Users
                .Select(user => new Result(
                    user,
                    db.UserRoles
                        .Where(userRole => userRole.UserId == user.Id)
                        .Join(db.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name!)
                        .ToList(),
                    db.Musicians
                        .Where(musician => musician.ApplicationUserId == user.Id)
                        .SelectMany(musician => musician.MusicianMusicParts)
                        .Include(musicianPart => musicianPart.MusicPart)
                        .ThenInclude(part => part.Aliases)
                        .OrderBy(musicianPart => musicianPart.MusicPart.SortOrder)
                        .Select(musicianPart => musicianPart.MusicPart)
                        .ToList()))
                .ToListAsync(cancellationToken);
        }
    }
}
