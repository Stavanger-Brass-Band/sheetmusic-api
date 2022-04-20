using Microsoft.AspNetCore.Authorization;

namespace SheetMusic.Api.Authorization;

public class AdministratorRequirement : IAuthorizationRequirement
{
    public AdministratorRequirement(string adminGroupName)
    {
        AdminGroupName = adminGroupName;
    }

    public string AdminGroupName { get; set; }
}
