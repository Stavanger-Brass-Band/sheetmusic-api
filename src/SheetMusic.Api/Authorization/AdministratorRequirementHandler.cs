using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using System;
using System.Threading.Tasks;

namespace SheetMusic.Api.Authorization;

public class AdministratorRequirementHandler(UserManager<ApplicationUser> userManager, SheetMusicContext dbContext, IMemoryCache memoryCache) : AuthorizationHandler<AdministratorRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdministratorRequirement requirement)
    {
        if (!context?.User?.Identity?.IsAuthenticated ?? false || context?.User?.Identity?.Name == null)
            return;

        if (!Guid.TryParse(context?.User?.Identity?.Name, out var userId))
            return;

        var cacheKey = $"{userId}_IsAdmin";

        if (memoryCache.TryGetValue<bool>(cacheKey, out var isAdmin))
        {
            if (isAdmin)
                context.Succeed(requirement);

            return;
        }

        // Try as ApplicationUser ID first (v2 tokens)
        var user = await userManager.FindByIdAsync(userId.ToString()!);

        // Fall back to Musician ID lookup (v1 tokens)
        if (user == null)
        {
            var musician = await dbContext.Musicians.FirstOrDefaultAsync(m => m.Id == userId);
            if (musician?.ApplicationUserId != null)
            {
                user = await userManager.FindByIdAsync(musician.ApplicationUserId.Value.ToString());
            }
        }

        isAdmin = user != null && await userManager.IsInRoleAsync(user, requirement.AdminGroupName);

        if (isAdmin)
            context.Succeed(requirement);

        memoryCache.Set(cacheKey, isAdmin, TimeSpan.FromMinutes(30));
    }
}
