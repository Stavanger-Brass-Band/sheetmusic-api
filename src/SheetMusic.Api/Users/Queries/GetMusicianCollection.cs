using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users.Queries;

public class GetMusicianCollection : IRequest<IReadOnlyList<GetMusicianCollection.Result>>
{
    public record Result(Musician Musician, IReadOnlyList<string> Roles);

    public class Handler(SheetMusicContext db) : IRequestHandler<GetMusicianCollection, IReadOnlyList<Result>>
    {
        public async Task<IReadOnlyList<Result>> Handle(GetMusicianCollection request, CancellationToken cancellationToken)
        {
            var musicians = await db.Musicians
                .AsNoTracking()
                .Include(musician => musician.ApplicationUser)
                .Include(musician => musician.MusicianMusicParts)
                    .ThenInclude(musicianPart => musicianPart.MusicPart)
                .Where(musician => musician.ApplicationUser != null
                    && !musician.ApplicationUser.Inactive
                    && musician.MusicianMusicParts.Any())
                .ToListAsync(cancellationToken);

            var userIds = musicians.Select(musician => musician.ApplicationUser!.Id).ToList();
            var roleAssignments = await db.UserRoles
                .Where(userRole => userIds.Contains(userRole.UserId))
                .Join(db.Roles, userRole => userRole.RoleId, role => role.Id, (userRole, role) => new { userRole.UserId, RoleName = role.Name! })
                .ToListAsync(cancellationToken);
            var rolesByUserId = roleAssignments
                .GroupBy(role => role.UserId)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.Select(role => role.RoleName).ToList());

            return musicians
                .Select(musician => new Result(
                    musician,
                    rolesByUserId.GetValueOrDefault(musician.ApplicationUser!.Id, [])))
                .ToList();
        }
    }
}