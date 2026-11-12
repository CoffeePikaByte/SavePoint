using GameHub.Application.Exceptions;

namespace GameHub.API.Middleware; 

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
        _next = next; 
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (ex is InvalidLibraryGameUpdateException)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { message = ex.Message });
            }
            else if(ex is UserNotFoundException or GameNotFoundException or LibraryGameNotFoundException)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsJsonAsync( new 
                {
                    message = ex.Message
                });

            }else if (ex is UserAlreadyExistsException or GameAlreadyInLibraryException)
            {
                context.Response.StatusCode = 409; 
                await context.Response.WriteAsJsonAsync( new
                {
                    message = ex.Message
                });
            }else
            {
                _logger.LogError(
                    ex,
                    "Ocurrio un error inesperado."
                );

                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new 
                {
                    message = "Ocurrio un error interno del servidor."
                });
            }
        }

    }


}