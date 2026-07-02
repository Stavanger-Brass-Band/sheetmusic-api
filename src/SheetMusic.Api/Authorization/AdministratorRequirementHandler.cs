using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using SheetMusic.Api.Database.Entities;
using System;
using System.Threading.Tasks;

namespace SheetMusic.Api.Authorization;

public class AdministratorRequirementHandler(UserManager<ApplicationUser> userManager, LegacyAuthResolver authResolver, IMemoryCache memoryCache) : AuthorizationHandler<AdministratorRequirement>
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

        var resolved = await authResolver.ResolveAsync(userId);

        if (resolved is null)
        {
            isAdmin = false;
        }
        else if (resolved.User != null)
        {
            isAdmin = await userManager.IsInRoleAsync(resolved.User, requirement.AdminGroupName);
        }
        else
        {
            // Legacy musician never linked to an ApplicationUser - fall back to its own UserGroup
#pragma warning disable CS0612 // Type or member is obsolete
            isAdmin = !resolved.LegacyMusician!.Inactive
                && string.Equals(resolved.LegacyMusician.UserGroup?.Name, requirement.AdminGroupName, StringComparison.OrdinalIgnoreCase);
#pragma warning restore CS0612
        }

        if (isAdmin)
            context.Succeed(requirement);

        memoryCache.Set(cacheKey, isAdmin, TimeSpan.FromMinutes(30));
    }
}

