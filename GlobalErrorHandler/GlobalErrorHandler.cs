namespace GlobalErrorHandler;

public static class ErrorHandlerExtensions
{
    public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlerMiddleware>();
    }
}

public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;
    private readonly ILoggerService _loggerService;

    public ErrorHandlerMiddleware(RequestDelegate next,
                                  ILogger<ErrorHandlerMiddleware> logger,
                                  ILoggerService loggerService)
    {
        _next = next;
        _logger = logger;
        _loggerService = loggerService;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await HandleExceptionAsync(ex, 404, context);
            return;
        }
        catch (BadRequestException ex)
        {
            await HandleExceptionAsync(ex, 400, context);
            return;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, 500, context);
            return;
        }
    }

    public async Task HandleExceptionAsync(Exception ex, int statusCode, HttpContext context)
    {
        #region Request informations
        var request = context.Request;
        var builder = new StringBuilder();
        builder.AppendLine($"Method: [{request.Method}]");
        string path = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
        builder.AppendLine($"Path: {path}");
        string ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "";

        if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            ipAddress = context.Request.Headers["X-Forwarded-For"]!;
            ipAddress = ipAddress.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
        }

        if (context.Request.Headers.ContainsKey("X-Real-IP") && string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = context.Request.Headers["X-Real-IP"]!;
        }

        if (context.Request.Headers.ContainsKey("REMOTE_ADDR") && string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = context.Request.Headers["REMOTE_ADDR"]!;
        }
        builder.AppendLine($"IP Address: {ipAddress}");
        builder.AppendLine($"User Agent: {request.Headers["User-Agent"]}");
        #endregion

        #region Log to telegram
        string source = ex.StackTrace?.Split("\n")[0] ?? "";
        string message = $"""
        🛑{ex.GetType().Name}: {ex.Message}
        Inner Exception: {ex.InnerException?.Message}
            
        🪲Source: {source}

        📡Request
        {builder}
        """;
        await _loggerService.ErrorAsync(message);
        #endregion

        #region response
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        var httpResponseException = new HttpResponseModel(status: statusCode, message: ex.Message, name: ex.GetType().Name);
        var result = JsonConvert.SerializeObject(httpResponseException);

        _logger.LogError(ex, ex.Message);
        await context.Response.WriteAsync(result);
        #endregion
    }
}