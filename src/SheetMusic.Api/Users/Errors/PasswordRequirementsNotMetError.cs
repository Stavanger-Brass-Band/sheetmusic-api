using Microsoft.AspNetCore.Identity;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Users.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace SheetMusic.Api.Users.Errors;

/// <summary>
/// Thrown when ASP.NET Core Identity rejects a password for not meeting the configured complexity
/// policy. Carries the failed <see cref="IdentityError.Code"/> values (stable and machine-readable, so
/// clients can highlight the exact failing rule instead of parsing prose), the corresponding
/// descriptions for direct display, and the policy itself so a client can render both with one type.
/// </summary>
public class PasswordRequirementsNotMetError(IReadOnlyCollection<IdentityError> identityErrors, ApiPasswordRequirements requirements)
    : ExceptionBase("The provided password does not meet the requirements.")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;

    public IReadOnlyList<string> FailedRequirements { get; } = identityErrors.Select(e => e.Code).ToList();
    public IReadOnlyList<string> Messages { get; } = identityErrors.Select(e => e.Description).ToList();
    public ApiPasswordRequirements Requirements { get; } = requirements;

    public override IDictionary<string, object?> Extensions => new Dictionary<string, object?>
    {
        ["failedRequirements"] = FailedRequirements,
        ["messages"] = Messages,
        ["requirements"] = Requirements
    };

    /// <summary>
    /// Identity's password validators (see <c>PasswordValidator&lt;TUser&gt;</c>) prefix every
    /// password-complexity error code with "Password" (e.g. <c>PasswordTooShort</c>,
    /// <c>PasswordRequiresDigit</c>). Used to separate those from unrelated <see cref="IdentityResult"/>
    /// failures (e.g. an invalid reset token) so each gets the right error type.
    /// </summary>
    public static bool IsPasswordError(IdentityError error) => error.Code?.StartsWith("Password", StringComparison.Ordinal) == true;

    /// <summary>
    /// Maps a failed <see cref="IdentityResult"/> to the appropriate error: this type when any failure
    /// is password-complexity related, or the generic <see cref="IdentityOperationError"/> otherwise.
    /// Shared by every v2 endpoint that runs a password through <see cref="Microsoft.AspNetCore.Identity.UserManager{TUser}"/>
    /// (register, update, reset), so the mapping itself lives in one place rather than in each caller.
    /// </summary>
    public static ExceptionBase FromFailedResult(IdentityResult result, ApiPasswordRequirements requirements)
    {
        var passwordErrors = result.Errors.Where(IsPasswordError).ToList();

        return passwordErrors.Count > 0
            ? new PasswordRequirementsNotMetError(passwordErrors, requirements)
            : new IdentityOperationError(result.Errors.Select(e => e.Description));
    }
}
