using FluentValidation;
using System.Linq;

namespace SheetMusic.Api.Users.RequestModels;

public class LoginRequest
{
    private string _grantType = "basic";

    /// <summary>
    /// The OAuth2 grant type. Either <c>"basic"</c> (username/password) or <c>"refresh_token"</c>.
    /// Defaults to <c>"basic"</c> when omitted, and also accepts the spec-correct <c>"password"</c>
    /// value (RFC 6749 resource owner password credentials grant), since OAuth2 clients implementing
    /// the documented password flow (e.g. Scalar) send <c>grant_type=password</c> rather than omitting it.
    /// </summary>
    public string grant_type
    {
        get => _grantType;
        set => _grantType = string.IsNullOrWhiteSpace(value) || value == "password" ? "basic" : value;
    }

    /// <summary>The account email address. Required when <see cref="grant_type"/> is <c>"basic"</c>.</summary>
    public string username { get; set; } = null!;

    /// <summary>The account password. Required when <see cref="grant_type"/> is <c>"basic"</c>.</summary>
    public string password { get; set; } = null!;

    /// <summary>A previously issued refresh token. Required when <see cref="grant_type"/> is <c>"refresh_token"</c>.</summary>
    public string? refresh_token { get; set; }

    // Optional
    //public string scope { get; set; }

    public class Validator : AbstractValidator<LoginRequest>
    {
        private static readonly string[] SupportedGrantTypes = ["basic", "refresh_token"];

        public Validator()
        {
            RuleFor(r => r.grant_type)
                .Must(g => SupportedGrantTypes.Contains(g)).WithMessage($"grant_type must be one of: {string.Join(", ", SupportedGrantTypes)}.");

            // The "refresh_token" grant exchanges a refresh token for a new token pair and needs no
            // credentials. The "basic" grant (the legacy/password grant this API accepts) requires
            // username/password.
            RuleFor(r => r.username).NotEmpty().WithMessage("username is required.").When(r => r.grant_type != "refresh_token");
            RuleFor(r => r.password).NotEmpty().WithMessage("password is required.").When(r => r.grant_type != "refresh_token");
            RuleFor(r => r.refresh_token).NotEmpty().WithMessage("refresh_token is required.").When(r => r.grant_type == "refresh_token");
        }
    }
}

