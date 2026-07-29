using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SheetMusic.Api.Configuration;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Users.Authorization;
using SheetMusic.Api.Users.Commands;
using SheetMusic.Api.Users.Errors;
using SheetMusic.Api.Users.Queries;
using SheetMusic.Api.Users.RequestModels;
using SheetMusic.Api.Users.ViewModels;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users;

/// <summary>
/// User management endpoints using ASP.NET Core Identity.
/// </summary>
[ApiVersion("2.0")]
[Authorize]
[ApiController]
public class UsersController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, IMediator mediator, IOptions<IdentityOptions> identityOptions) : ControllerBase
{
    /// <summary>
    /// Authenticate using Identity and receive a JWT token.
    /// </summary>
    /// <param name="request">The username and password to authenticate with</param>
    /// <response code="200">The access token</response>
    /// <response code="400">Username or password is incorrect</response>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Token)]
    [ProducesResponseType(typeof(ApiAccessTokens), (int)HttpStatusCode.OK)]
    [HttpPost("token")]
    public async Task<IActionResult> AuthenticateAsync([FromForm] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.username);

        if (user == null || user.Inactive)
            return BadRequest(new { message = "Username or password is incorrect" });

        // lockoutOnFailure: true increments the failed access count and locks the account after
        // IdentityOptions.Lockout.MaxFailedAccessAttempts is reached. The response is intentionally
        // identical to the generic invalid-credentials case to avoid leaking account lockout state
        // to an unauthenticated caller.
        var result = await signInManager.CheckPasswordSignInAsync(user, request.password, lockoutOnFailure: true);

        if (!result.Succeeded)
            return BadRequest(new { message = "Username or password is incorrect" });

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(configuration[ConfigKeys.JwtSigningKey] ?? throw new MissingConfigurationException(ConfigKeys.JwtSigningKey));
        var expires = DateTime.UtcNow.AddDays(7);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.Name, user.Id.ToString())]),
            Expires = expires,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(new ApiAccessTokens
        {
            expires_in = (expires - DateTime.UtcNow).Seconds,
            access_token = tokenString,
            token_type = "bearer",
            scope = "sheetmusic-api"
        });
    }

    /// <summary>
    /// Register a new user. User is created as inactive and must be activated by an admin.
    /// </summary>
    /// <param name="request">Details about the new user</param>
    /// <response code="201">Details about the newly created user</response>
    /// <response code="400">If provided input is invalid. A password that does not meet the requirements
    /// returned by <c>GET users/password-requirements</c> produces a <see cref="PasswordRequirementsNotMetError"/></response>
    [AllowAnonymous]
    [HttpPost("users/register")]
    public async Task<IActionResult> RegisterAsync([FromBody] UserRequest request)
    {
        var user = new ApplicationUser
        {
            Id = request.Id ?? Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.Name,
            Inactive = true
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            throw PasswordRequirementsNotMetError.FromFailedResult(result, ApiPasswordRequirements.FromPasswordOptions(identityOptions.Value.Password));

        await userManager.AddToRoleAsync(user, "Reader");

        return new CreatedResult("users", new ApiUser(user));
    }

    /// <summary>
    /// Update a user's password. Admins can update any user.
    /// </summary>
    /// <param name="identifier">The guid of the user to update</param>
    /// <param name="request">The new password</param>
    /// <response code="200">Password was updated successfully</response>
    /// <response code="400">Unable to identify the authenticated user, or the new password does not
    /// meet the requirements returned by <c>GET users/password-requirements</c> (<see cref="PasswordRequirementsNotMetError"/>).
    /// The existing password remains valid in that case.</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. Only the user themselves or an Administrator can update the password</response>
    /// <response code="404">User not found</response>
    [HttpPut("users/{identifier}")]
    public async Task<IActionResult> UpdateUser(Guid identifier, [FromBody] UpdateUserRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.Name), out Guid authenticatedUserId))
            return BadRequest("Unable to find Name claim and identify user");

        var currentUser = await userManager.FindByIdAsync(authenticatedUserId.ToString());
        var isAdmin = currentUser != null && await userManager.IsInRoleAsync(currentUser, "Admin");
        var userToChange = await userManager.FindByIdAsync(identifier.ToString());

        if (userToChange == null)
            return NotFound();

        if (authenticatedUserId != identifier && !isAdmin)
            return Forbid();

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(userToChange);
            var result = await userManager.ResetPasswordAsync(userToChange, token, request.Password);

            if (!result.Succeeded)
                throw PasswordRequirementsNotMetError.FromFailedResult(result, ApiPasswordRequirements.FromPasswordOptions(identityOptions.Value.Password));
        }

        return Ok();
    }

    /// <summary>
    /// Get the password complexity requirements enforced when registering, updating a password, or
    /// resetting a password. Backed by the same configured policy used to enforce those rules, so a
    /// client can render a requirements checklist before submission.
    /// </summary>
    /// <response code="200">The configured password requirements</response>
    [AllowAnonymous]
    [HttpGet("users/password-requirements")]
    public async Task<ActionResult<ApiPasswordRequirements>> GetPasswordRequirementsAsync()
    {
        var requirements = await mediator.Send(new GetPasswordRequirements());

        return Ok(requirements);
    }

    /// <summary>
    /// Get all users. Admin only.
    /// </summary>
    /// <response code="200">A list of all users</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpGet("users")]
    public async Task<IActionResult> GetAll()
    {
        var users = await mediator.Send(new GetUserCollection());

        return Ok(users.Select(u => new ApiUser(u)));
    }

    /// <summary>
    /// Get a user by ID or "me" for the current user. Admins can view any user; other users may only view themselves.
    /// </summary>
    /// <param name="identifier">The guid of the user, or "me" for the current user</param>
    /// <response code="200">The user details, including assigned roles</response>
    /// <response code="400">Identifier could not be parsed, or the authenticated user could not be identified</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. Only the user themselves or an Administrator can view this user</response>
    /// <response code="404">User not found</response>
    [HttpGet("users/{identifier}")]
    public async Task<IActionResult> GetByIdAsync(string identifier)
    {
        if (identifier == "me")
        {
            identifier = HttpContext?.User?.Identity?.Name ?? string.Empty;
        }

        if (!Guid.TryParse(identifier, out var id))
            return BadRequest(new ProblemDetails { Title = "Unable to parse identifier" });

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.Name), out var authenticatedUserId))
            return BadRequest("Unable to find Name claim and identify user");

        if (authenticatedUserId != id)
        {
            var currentUser = await userManager.FindByIdAsync(authenticatedUserId.ToString());
            var isAdmin = currentUser != null && await userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isAdmin)
                return Forbid();
        }

        var result = await mediator.Send(new GetUser(id));

        return Ok(new ApiUserDetail(result.User, result.Roles));
    }

    /// <summary>
    /// Activate a user, allowing them to log in. Admin only.
    /// </summary>
    /// <param name="id">The guid of the user to activate</param>
    /// <response code="200">User was activated successfully</response>
    /// <response code="400">The identity operation failed</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    /// <response code="404">User not found</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpPut("users/{id}/activate")]
    public async Task<IActionResult> ActivateUserAsync(Guid id)
    {
        await mediator.Send(new ActivateUser(id));
        return Ok();
    }

    /// <summary>
    /// Deactivate a user, preventing them from logging in. Admin only.
    /// </summary>
    /// <param name="id">The guid of the user to deactivate</param>
    /// <response code="200">User was deactivated successfully</response>
    /// <response code="400">The identity operation failed</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    /// <response code="404">User not found</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpPut("users/{id}/deactivate")]
    public async Task<IActionResult> DeactivateUserAsync(Guid id)
    {
        await mediator.Send(new DeactivateUser(id));
        return Ok();
    }

    /// <summary>
    /// Assign a role to a user. Admin only.
    /// </summary>
    /// <param name="id">The guid of the user to assign the role to</param>
    /// <param name="request">The name of the role to assign</param>
    /// <response code="200">Role was assigned successfully</response>
    /// <response code="400">The identity operation failed</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    /// <response code="404">User or role not found</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpPut("users/{id}/roles")]
    public async Task<IActionResult> AssignRoleAsync(Guid id, [FromBody] AssignRoleRequest request)
    {
        await mediator.Send(new AssignRole(id, request.RoleName));
        return Ok();
    }

    /// <summary>
    /// Remove a role from a user. Admin only.
    /// </summary>
    /// <param name="id">The guid of the user to remove the role from</param>
    /// <param name="roleName">The name of the role to remove</param>
    /// <response code="204">Role was removed successfully</response>
    /// <response code="400">The identity operation failed</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    /// <response code="404">User not found</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpDelete("users/{id}/roles/{roleName}")]
    public async Task<IActionResult> RemoveRoleAsync(Guid id, string roleName)
    {
        await mediator.Send(new RemoveRole(id, roleName));
        return NoContent();
    }

    /// <summary>
    /// Delete a user. Defaults to a soft delete (deactivation). Pass <paramref name="hardDelete"/>=true to permanently remove the user. Admin only.
    /// </summary>
    /// <param name="id">The guid of the user to delete</param>
    /// <param name="hardDelete">If true, permanently removes the user instead of deactivating it</param>
    /// <response code="204">User was deleted successfully</response>
    /// <response code="400">The identity operation failed</response>
    /// <response code="401">Authorization header (bearer token) is invalid</response>
    /// <response code="403">Forbidden. User does not have required privileges (Administrator)</response>
    /// <response code="404">User not found</response>
    [Authorize(AuthPolicy.Admin)]
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUserAsync(Guid id, [FromQuery] bool hardDelete = false)
    {
        await mediator.Send(new DeleteUser(id, hardDelete));
        return NoContent();
    }

    /// <summary>
    /// Request a password reset email. Always returns 200 to prevent user enumeration.
    /// Rate limited per client IP to prevent abuse of the outbound email flow.
    /// </summary>
    /// <param name="request">The email address of the user requesting a reset</param>
    /// <response code="200">Request was accepted (regardless of whether the email is registered)</response>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.ForgotPassword)]
    [HttpPost("users/forgot-password")]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest request)
    {
        await mediator.Send(new RequestPasswordReset(request.Email));
        return Ok();
    }

    /// <summary>
    /// Reset a user's password using a token received via email.
    /// </summary>
    /// <param name="request">The email, reset token and new password</param>
    /// <response code="200">Password was reset successfully</response>
    /// <response code="400">The reset token is invalid or expired</response>
    [AllowAnonymous]
    [HttpPost("users/reset-password")]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest request)
    {
        await mediator.Send(new ResetPassword(request.Email, request.Token, request.NewPassword));
        return Ok();
    }
}
