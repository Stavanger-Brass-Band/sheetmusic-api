using System;
using System.Collections.Generic;
using System.Net;

namespace SheetMusic.Api.Errors;

public class ExceptionBase : Exception
{
    public ExceptionBase(string message) : base(message)
    {
    }

    public ExceptionBase(string message, Exception innerException) : base(message, innerException)
    {
    }

    public virtual HttpStatusCode StatusCode => HttpStatusCode.InternalServerError;

    /// <summary>
    /// Additional machine-readable data to merge into the <c>ProblemDetails</c> response as extension
    /// members (e.g. <see cref="Users.Errors.PasswordRequirementsNotMetError"/>'s failed requirement
    /// codes). Null by default, since most errors need only the base ProblemDetails fields.
    /// </summary>
    public virtual IDictionary<string, object?>? Extensions => null;
}
