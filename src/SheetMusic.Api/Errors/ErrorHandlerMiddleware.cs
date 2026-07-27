using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SheetMusic.Api.Errors;

public class ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next.Invoke(context);
        }
        catch (ExceptionBase eb)
        {
            logger.LogError(eb, eb.Message);

            var errorType = eb.GetType().Name;
            var error = new ProblemDetails { Status = (int)eb.StatusCode, Type = errorType, Title = errorType, Detail = eb.Message };

            await WriteProblemDetailsAsync(context, error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error occured");

            // Never echo the exception details back to the caller - they may contain sensitive information.
            var error = new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Type = "InternalServerError",
                Title = "InternalServerError",
                Detail = "An unexpected error occurred while processing the request."
            };

            await WriteProblemDetailsAsync(context, error);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, ProblemDetails error)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = error.Status ?? (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(error));
    }
}
