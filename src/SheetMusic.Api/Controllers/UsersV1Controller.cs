using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SheetMusic.Api.Authorization;
using SheetMusic.Api.Configuration;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.Controllers.ViewModels;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Repositories;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SheetMusic.Api.Controllers;

/// <summary>
/// Legacy user endpoints using custom HMAC authentication. Deprecated — use API version 2.0.
/// </summary>
[ApiVersion("1.0", Deprecated = true)]
[Authorize(AuthPolicy.Admin)]
[ApiController]
public class UsersV1Controller(IUserRepository userRepository, IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Authenticate using legacy HMAC password hash and receive a JWT token.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Token)]
    [ProducesResponseType(typeof(ApiAccessTokens), (int)HttpStatusCode.OK)]
    [HttpPost("token")]
    public async Task<IActionResult> AuthenticateAsync([FromForm] LoginRequest request)
    {
#pragma warning disable CS0612
        var user = await userRepository.AuthenticateAsync(request.username, request.password);
#pragma warning restore CS0612

        if (user == null)
            return BadRequest(new { message = "Username or password is incorrect" });

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(configuration[ConfigKeys.Secret] ?? throw new MissingConfigurationException(ConfigKeys.Secret));
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
    /// Register a new user using legacy password hashing.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("users/register")]
    public async Task<IActionResult> RegisterAsync([FromBody] UserRequest request)
    {
        if (!request.Id.HasValue)
        {
            request.Id = Guid.NewGuid();
        }

#pragma warning disable CS0612
        var user = new Musician
        {
            Id = request.Id.Value,
            Name = request.Name,
            Email = request.Email,
            UserGroupId = Guid.Parse("307E4C23-FFD0-4350-9CF0-F5A80097C4D9"),
            Inactive = true
        };

        user = await userRepository.CreateAsync(user, request.Password);

        return new CreatedResult("users", new ApiUser(user));
#pragma warning restore CS0612
    }

    /// <summary>
    /// Update a user's password using legacy password hashing.
    /// </summary>
    [HttpPut("users/{identifier}")]
    public async Task<IActionResult> UpdateUser(Guid identifier, [FromBody] UpdateUserRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.Name), out Guid authenticatedUserId))
            return BadRequest("Unable to find Name claim and identify user");

#pragma warning disable CS0612
        var currentUser = await userRepository.GetByIdAsync(authenticatedUserId);
        var isAdmin = currentUser.UserGroup?.Name?.ToLower() == "admin";
        var userToChange = await userRepository.GetByIdAsync(identifier);
#pragma warning restore CS0612

        if (authenticatedUserId != identifier && !isAdmin)
            return BadRequest(new { message = "Cannot update other users than yourself unless you are admin" });

#pragma warning disable CS0612
        await userRepository.UpdateAsync(userToChange, request.Password);
#pragma warning restore CS0612

        return Ok();
    }

    /// <summary>
    /// Get all users.
    /// </summary>
    [HttpGet("users")]
    public IActionResult GetAll()
    {
#pragma warning disable CS0612
        var users = userRepository.GetAll();
        var apiUsers = users.Select(u => new ApiUser(u));
#pragma warning restore CS0612

        return Ok(apiUsers);
    }

    /// <summary>
    /// Get a user by ID or "me" for the current user.
    /// </summary>
    [HttpGet("users/{identifier}")]
    public async Task<IActionResult> GetByIdAsync(string identifier)
    {
        if (identifier == "me")
        {
            identifier = HttpContext?.User?.Identity?.Name ?? string.Empty;
        }

        if (Guid.TryParse(identifier, out var id))
        {
#pragma warning disable CS0612
            var user = await userRepository.GetByIdAsync(id);
            var apiUser = new ApiUser(user);
#pragma warning restore CS0612

            return Ok(apiUser);
        }
        else
        {
            return BadRequest(new ProblemDetails { Title = "Unable to parse identifier" });
        }
    }
}
