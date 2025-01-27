namespace GlobalErrorHandler;

public static class ErrorHandlerExtensions
{
    public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlerMiddleware>();
    }
}

public class ErrorHandlerMiddleware(RequestDelegate next,
                                  ILogger<ErrorHandlerMiddleware> logger,
                                  ILoggerService loggerService)
{
    public async Task Invoke(HttpContext context)
    {
        CancellationTokenSource cts = new();
        cts.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            await HandleExceptionAsync(ex, 404, context, cts.Token);
            return;
        }
        catch (BadRequestException ex)
        {
            await HandleExceptionAsync(ex, 400, context, cts.Token);
            return;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, 500, context, cts.Token);
            return;
        }
    }

    public async Task HandleExceptionAsync(Exception ex, int statusCode, HttpContext context, CancellationToken cancellationToken = default)
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
        var requestData = await CollectRequestDataAsync(context, cancellationToken);
        await loggerService.ErrorAsync(message, requestData, cancellationToken);
        #endregion

        #region response
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        var httpResponseException = new HttpResponseModel(status: statusCode, message: ex.Message, name: ex.GetType().Name);
        var result = JsonConvert.SerializeObject(httpResponseException);

        logger.LogError(ex, ex.Message);
        await context.Response.WriteAsync(result, cancellationToken);
        #endregion
    }

    public async Task<byte[]> CollectRequestDataAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        // Allow the request body to be re-read
        context.Request.EnableBuffering();

        var requestData = new RequestData
        {
            Method = context.Request.Method,
            Path = context.Request.Path,
            Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            QueryString = context.Request.QueryString.ToString()
        };

        // Read the request body if it exists
        if (context.Request.ContentLength > 0)
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync(cancellationToken);

            // Store the body in the RequestData object
            requestData.Body = body;

            // Rewind the stream so it can be read later by other middleware
            context.Request.Body.Seek(0, SeekOrigin.Begin);
        }

        // Now you can use requestData as needed
        var data = JsonConvert.SerializeObject(requestData);
        return Encoding.UTF8.GetBytes(data);
    }
}