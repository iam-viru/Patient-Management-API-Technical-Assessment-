using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PatientManagement.Api.ErrorHandling;

/// <summary>
/// Last-resort handler for unhandled exceptions: logs the full error server-side and returns a generic
/// 500 ProblemDetails, so stack traces, SQL errors or connection strings never reach the client.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    // Non-standard code (popularised by nginx) for "client closed the request"; it is only ever logged.
    public const int ClientClosedRequestStatusCode = 499;

    public const string GenericErrorTitle = "An unexpected error occurred.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // The caller hung up, so this is not a server fault and there is nobody left to send a body to.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request {Method} {Path} was cancelled by the client.",
                httpContext.Request.Method, httpContext.Request.Path);
            httpContext.Response.StatusCode = ClientClosedRequestStatusCode;
            return true;
        }

        logger.LogError(exception, "Unhandled exception for {Method} {Path} (trace id {TraceId}).",
            httpContext.Request.Method, httpContext.Request.Path, httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = GenericErrorTitle,
                Detail = "Please try again later. Quote the trace id if you contact support."
            }
        });
    }
}
