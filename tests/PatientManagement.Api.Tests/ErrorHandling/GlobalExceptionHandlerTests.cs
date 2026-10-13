using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using PatientManagement.Api.ErrorHandling;

namespace PatientManagement.Api.Tests.ErrorHandling;

public class GlobalExceptionHandlerTests
{
    private const string SecretMessage = "Server=tcp:secret-db;Password=hunter2";

    [Fact]
    public async Task TryHandleAsync_UnhandledException_Writes500ProblemDetailsWithoutExceptionMessage()
    {
        // Arrange
        var (handler, httpContext) = CreateHandlerAndContext();

        // Act
        var handled = await handler.TryHandleAsync(
            httpContext, new InvalidOperationException(SecretMessage), CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        var body = ReadBody(httpContext);
        Assert.DoesNotContain("hunter2", body);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, JsonSerializerOptions.Web);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem!.Status);
        Assert.Equal(GlobalExceptionHandler.GenericErrorTitle, problem.Title);
    }

    [Fact]
    public async Task TryHandleAsync_ClientAbortedRequest_Returns499WithoutBody()
    {
        // Arrange
        var (handler, httpContext) = CreateHandlerAndContext();
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        httpContext.RequestAborted = aborted.Token;

        // Act
        var handled = await handler.TryHandleAsync(
            httpContext, new OperationCanceledException(aborted.Token), CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(GlobalExceptionHandler.ClientClosedRequestStatusCode, httpContext.Response.StatusCode);
        Assert.Empty(ReadBody(httpContext));
    }

    private static (GlobalExceptionHandler Handler, DefaultHttpContext HttpContext) CreateHandlerAndContext()
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Response.Body = new MemoryStream();

        var handler = new GlobalExceptionHandler(
            services.GetRequiredService<IProblemDetailsService>(), NullLogger<GlobalExceptionHandler>.Instance);
        return (handler, httpContext);
    }

    private static string ReadBody(HttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        return new StreamReader(httpContext.Response.Body).ReadToEnd();
    }
}
