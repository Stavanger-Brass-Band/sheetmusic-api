using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Query;

public class GetUserCollection : IRequest<IReadOnlyList<ApplicationUser>>
{
    public class Handler(UserManager<ApplicationUser> userManager) : IRequestHandler<GetUserCollection, IReadOnlyList<ApplicationUser>>
    {
        public async Task<IReadOnlyList<ApplicationUser>> Handle(GetUserCollection request, CancellationToken cancellationToken)
        {
            return await userManager.Users.ToListAsync(cancellationToken);
        }
    }
}
