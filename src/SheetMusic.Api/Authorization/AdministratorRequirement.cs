using Microsoft.AspNetCore.Authorization;

namespace SheetMusic.Api.Authorization;

public class AdministratorRequirement(string adminGroupName) : IAuthorizationRequirement
{
    public string AdminGroupName { get; set; } = adminGroupName;
}
