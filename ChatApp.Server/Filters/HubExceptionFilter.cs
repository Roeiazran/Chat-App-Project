namespace ChatApp.Server.Filters;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

public class HubExceptionFilter : IHubFilter
{
    private readonly ILogger<HubExceptionFilter> _logger;

    public HubExceptionFilter(ILogger<HubExceptionFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex)
        {
            string message = ex.Message;
            string errorType = ex.GetType().Name;

            switch (ex)
            {
                case UnauthorizedAccessException:
                    message = ex.Message;
                    break;

                case ArgumentException argEx:
                    message = argEx.Message;
                    break;

                default:
                    message = $"[HubExceptionFilter] {invocationContext.HubMethodName} failed: {ex.Message}";
                    break;
            }
            
            _logger.LogError(ex, "Hub method {HubMethod} failed: {Message}", 
                invocationContext.HubMethodName, message);

            // Throw HubException with serialized error
            throw new HubException(SerializeError(message, errorType));
        }
    }

    private static string SerializeError(string message, string errorType)
    {
        
        return JsonSerializer.Serialize(new
        {
            message,
            error = errorType
        });
    }
}
