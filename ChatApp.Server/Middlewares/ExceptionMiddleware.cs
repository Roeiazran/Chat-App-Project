namespace ChatApp.Server.Middlewares;

using System.Data.SqlClient;
using System.Net;
using System.Text.Json;

/// <summary>
/// Global exception handler for Http requests.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Calls the next middleware and catches his errors.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles exceptions by setting an appropriate HTTP status code and returning a JSON error response.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <returns>A task representing the writing the error response.</returns>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        HttpStatusCode status;
        string message;

        switch (exception)
        {
            case UnauthorizedAccessException:
                status = HttpStatusCode.Unauthorized;
                message = exception.Message;
                break;
            
            case SqlException:
                status = HttpStatusCode.Conflict;
                message = exception.Message;
                break;
            
            case ArgumentException argEx:
                status = HttpStatusCode.BadRequest;
                message = argEx.Message;
                break;

            default:
                status = HttpStatusCode.InternalServerError;
                message = "An unexpected error occurred.";
                break;
        }

        // log the error
        Console.Error.WriteLine($"Path: {context.Request.Path}, {exception.GetType().Name}: {exception.Message}");

        // create JSON string.
        var result = JsonSerializer.Serialize(new
        {
            message,
            error = exception.GetType().Name
        });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        return context.Response.WriteAsync(result);
    }
}
