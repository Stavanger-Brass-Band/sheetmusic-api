using SheetMusic.Api.Database.Entities;
using System.Collections.Generic;
using System.Linq;

namespace SheetMusic.Api.Controllers.ViewModels;

public class ApiUserDetail : ApiUser
{
    public ApiUserDetail(ApplicationUser user, IEnumerable<string> roles) : base(user)
    {
        Roles = roles.ToList();
    }

    public IReadOnlyList<string> Roles { get; }
}
