using GameHub.API.Middleware;
using GameHub.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameHub.API.Tests;

public class ExceptionHandlingMiddlewareTests
{
    [Theory]
    [InlineData("missing-user", 404, "known error")]
    [InlineData("existing-user", 409, "known error")]
    [InlineData("unexpected", 500, "Ocurrio un error interno del servidor.")]
    public async Task InvokeAsync_PreservesExistingErrorContracts(string kind, int status, string message)
    {
        Exception error = kind switch
        {
            "missing-user" => new UserNotFoundException("known error"),
            "existing-user" => new UserAlreadyExistsException("known error"),
            _ => new InvalidOperationException("private diagnostic")
        };
        var middleware = new ExceptionHandlingMiddleware(_ => throw error,
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context);
        Assert.Equal(status, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var json = await System.Text.Json.JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(message, json.RootElement.GetProperty("message").GetString());
    }
}
