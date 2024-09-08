using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SheetMusic.Api.Database;
using System;
using System.Threading.Tasks;

namespace SheetMusic.Api.Authorization;

public class AdministratorRequirementHandler(SheetMusicContext dbContext, IMemoryCache memoryCache) : AuthorizationHandler<AdministratorRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdministratorRequirement requirement)
    {
        if (!context?.User?.Identity?.IsAuthenticated ?? false || context?.User?.Identity?.Name == null)
            return;
        Guid? userId;

        if (Guid.TryParse(context?.User?.Identity?.Name, out var result))
        {
            userId = result;
        }
        else
        {
            return;
        }

        var cacheKey = $"{userId}_IsAdmin";

        if (memoryCache.TryGetValue<bool>(cacheKey, out var isAdmin))
        {
            if (isAdmin)
                context.Succeed(requirement);

            return;
        }

        var dbUser = await dbContext.Musicians
            .Include(m => m.UserGroup)
            .FirstOrDefaultAsync(m => m.Id == userId);

        isAdmin = dbUser?.UserGroup?.Name == requirement.AdminGroupName;

        if (isAdmin)
            context.Succeed(requirement);

        memoryCache.Set(cacheKey, isAdmin, TimeSpan.FromMinutes(30));
    }
}
