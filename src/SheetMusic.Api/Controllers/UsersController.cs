using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SheetMusic.Api.Authorization;
using SheetMusic.Api.Controllers.RequestModels;
using SheetMusic.Api.Controllers.ViewModels;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Repositories;
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
public class UsersController(IUserRepository userRepository, IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiAccessTokens), (int)HttpStatusCode.OK)]
    [HttpPost("token")]
    public async Task<IActionResult> AuthenticateAsync([FromForm] LoginRequest request)
    {
        var user = await userRepository.AuthenticateAsync(request.username, request.password);

        if (user == null)
            return BadRequest(new { message = "Username or password is incorrect" });

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(configuration["AppSettings:Secret"]);
        var expires = DateTime.UtcNow.AddDays(7);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, user.Id.ToString())
            }),
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
        if (!request.Id.HasValue)
        {
            request.Id = Guid.NewGuid();
        }

        var user = new Musician
        {
            Id = request.Id.Value,
            Name = request.Name,
            Email = request.Email,
            UserGroupId = Guid.Parse("307E4C23-FFD0-4350-9CF0-F5A80097C4D9"), //reader, hardcoded for now
            Inactive = true //inactive until manually activated
        };

        user = await userRepository.CreateAsync(user, request.Password);

        return new CreatedResult("users", new ApiUser(user));
    }

    [HttpGet("users")]
    public IActionResult GetAll()
    {
        var users = userRepository.GetAll();
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
            var user = await userRepository.GetByIdAsync(id);
            var apiUser = new ApiUser(user);

            return Ok(apiUser);
        }
        else
        {
            return BadRequest(new ProblemDetails { Title = "Unable to parse identifier" });
        }
    }
}
