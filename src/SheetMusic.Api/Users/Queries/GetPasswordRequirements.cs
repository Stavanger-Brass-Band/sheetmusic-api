using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SheetMusic.Api.Users.ViewModels;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users.Queries;

/// <summary>
/// Retrieves the password complexity policy from the configured <see cref="IdentityOptions"/>, so the
/// advertised policy can never disagree with what <see cref="UserManager{TUser}"/> actually enforces.
/// </summary>
public class GetPasswordRequirements : IRequest<ApiPasswordRequirements>
{
    public class Handler(IOptions<IdentityOptions> identityOptions) : IRequestHandler<GetPasswordRequirements, ApiPasswordRequirements>
    {
        public Task<ApiPasswordRequirements> Handle(GetPasswordRequirements request, CancellationToken cancellationToken)
        {
            return Task.FromResult(ApiPasswordRequirements.FromPasswordOptions(identityOptions.Value.Password));
        }
    }
}
