using MediatR;
using Microsoft.AspNetCore.Identity;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Query;

public class GetUser(Guid userId) : IRequest<GetUser.Result>
{
    public Guid UserId { get; } = userId;

    public record Result(ApplicationUser User, IReadOnlyList<string> Roles);

    public class Handler(UserManager<ApplicationUser> userManager) : IRequestHandler<GetUser, Result>
    {
        public async Task<Result> Handle(GetUser request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(request.UserId.ToString())
                ?? throw new NotFoundError($"users/{request.UserId}", "User not found");

            var roles = await userManager.GetRolesAsync(user);

            return new Result(user, roles.ToList());
        }
    }
}
