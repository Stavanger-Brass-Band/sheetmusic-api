using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SheetMusic.Api.Authorization;
using SheetMusic.Api.Configuration;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.Controllers.ViewModels;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SheetMusic.Api.Controllers;

[Authorize(AuthPolicy.Admin)]
[ApiController]
public class UsersController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiAccessTokens), (int)HttpStatusCode.OK)]
    [HttpPost("token")]
    public async Task<IActionResult> AuthenticateAsync([FromForm] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.username);

        if (user == null || user.Inactive)
            return BadRequest(new { message = "Username or password is incorrect" });

        var result = await signInManager.CheckPasswordSignInAsync(user, request.password, lockoutOnFailure: false);

        if (!result.Succeeded)
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
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await userManager.AddToRoleAsync(user, "Reader");

        return new CreatedResult("users", new ApiUser(user));
    }

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
            return BadRequest(new { message = "Cannot update other users than yourself unless you are admin" });

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(userToChange);
            await userManager.ResetPasswordAsync(userToChange, token, request.Password);
        }

        return Ok();
    }

    [HttpGet("users")]
    public IActionResult GetAll()
    {
        var users = userManager.Users.ToList();
        var apiUsers = users.Select(u => new ApiUser(u));

        return Ok(apiUsers);
    }

    [HttpGet("users/{identifier}")]
    public async Task<IActionResult> GetByIdAsync(string identifier)
    {
        if (identifier == "me")
        {
            identifier = HttpContext?.User?.Identity?.Name ?? string.Empty;
        }

        if (Guid.TryParse(identifier, out var id))
        {
            var user = await userManager.FindByIdAsync(id.ToString());

            if (user == null)
                return NotFound();

            return Ok(new ApiUser(user));
        }
        else
        {
            return BadRequest(new ProblemDetails { Title = "Unable to parse identifier" });
        }
    }
}
