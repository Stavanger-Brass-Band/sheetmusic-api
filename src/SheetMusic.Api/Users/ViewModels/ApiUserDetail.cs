using SheetMusic.Api.Users.Entities;
using System.Collections.Generic;
using System.Linq;

namespace SheetMusic.Api.Users.ViewModels;

public class ApiUserDetail : ApiUser
{
    public ApiUserDetail(ApplicationUser user, IEnumerable<string> roles) : base(user)
    {
        Roles = roles.ToList();
    }

    public IReadOnlyList<string> Roles { get; }
}
