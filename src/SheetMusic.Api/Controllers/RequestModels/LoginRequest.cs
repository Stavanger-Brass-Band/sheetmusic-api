using System.ComponentModel.DataAnnotations;

namespace SheetMusic.Api.Controllers.RequestModels;

public class LoginRequest
{
    public string grant_type { get; set; } = null!;
    public string username { get; set; } = null!;
    public string password { get; set; } = null!;
    public string? refresh_token { get; set; }

    // Optional
    //public string scope { get; set; }
}
