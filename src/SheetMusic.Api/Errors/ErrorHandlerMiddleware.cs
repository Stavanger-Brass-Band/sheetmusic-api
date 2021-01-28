using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SheetMusic.Api.Errors
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> logger;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
        {
            _next = next;
            this.logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (ExceptionBase eb)
            {
                var errorType = eb.GetType().Name;
                var error = new ProblemDetails { Status = (int)eb.StatusCode, Type = errorType, Title = errorType, Detail = eb.Message };

                context.Response.StatusCode = (int)eb.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonConvert.SerializeObject(error));
                logger.LogError(eb, eb.Message);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                logger.LogError(ex, "Unhandled error occured");
            }
        }
    }
}
