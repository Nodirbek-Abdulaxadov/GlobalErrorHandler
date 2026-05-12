using Microsoft.Extensions.Hosting;
using System.Text;

namespace GlobalErrorHandler;

public static class ErrorHandlerExtensions
{
    /// <summary>
    /// Adds <see cref="ErrorHandlerMiddleware"/> to the pipeline. Make sure to also call
    /// <c>services.AddGlobalErrorHandler()</c> so the required <see cref="IErrorHandlerSink"/>
    /// is registered (defaults to a no-op sink).
    /// </summary>
    public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlerMiddleware>();
    }
}

public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ErrorHandlerMiddleware> logger;
    private readonly IErrorHandlerSink sink;
    private readonly IHostEnvironment hostEnvironment;

    /// <summary>
    /// Preferred constructor (v2.1.0+). Errors are published via the registered
    /// <see cref="IErrorHandlerSink"/>.
    /// </summary>
    public ErrorHandlerMiddleware(RequestDelegate next,
                                  ILogger<ErrorHandlerMiddleware> logger,
                                  IErrorHandlerSink sink,
                                  IHostEnvironment hostEnvironment)
    {
        this.next = next;
        this.logger = logger;
        this.sink = sink;
        this.hostEnvironment = hostEnvironment;
    }

    public async Task Invoke(HttpContext context)
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        // Read request body up-front (instance-local — not shared across requests).
        JObject? requestBody = null;
        try
        {
            requestBody = await ReadRequestBodyAsync(context.Request, cts.Token);
            await next(context);
        }
        catch (Exception ex)
        {
            // Route every exception through the registry. Defaults for built-in
            // exception types (BadRequest=400, NotFound=404, PermissionDenied=403)
            // are seeded in ExceptionHandler's static ctor and can be overridden.
            int statusCode = ExceptionHandler.TryGetStatusCode(ex, out int mapped) ? mapped : 500;
            await HandleExceptionAsync(ex, statusCode, context, requestBody, cts.Token);
        }
    }

    /// <summary>
    /// Build the report and publish it through the registered <see cref="IErrorHandlerSink"/>.
    /// </summary>
    public async Task HandleExceptionAsync(
        Exception ex,
        int statusCode,
        HttpContext context,
        JObject? requestBody,
        CancellationToken cancellationToken = default)
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

        #region Build report
        var env = hostEnvironment.EnvironmentName;
        // Inverted from the original: append InnerException.Message when it is non-null,
        // not when it is null (the previous condition was reversed).
        string innerMsg = ex.InnerException != null ? ex.InnerException.Message : "";
        string title =
@$"🛑`{ex.GetType().Name}: {ex.Message} {innerMsg}`

🌐 Environment: *{env}*
        ";

        var requestData = CollectRequestData(context, ex, requestBody);

        try
        {
            await sink.PublishAsync(title, ex, requestData, cancellationToken);
        }
        catch (Exception sinkEx)
        {
            // Never let a logging sink failure take down the response path.
            logger.LogError(sinkEx, "IErrorHandlerSink failed to publish error report.");
        }
        #endregion

        #region response
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        var httpResponseException = new HttpResponseModel(code: statusCode, message: ex.Message, status: ex.GetType().Name);
        var result = JsonConvert.SerializeObject(httpResponseException);

        logger.LogError(ex, ex.Message);
        await context.Response.WriteAsync(result, cancellationToken);
        #endregion
    }

    /// <summary>
    /// Legacy entry point kept for binary compatibility with v2.0.x. Prefer the overload
    /// that takes the captured request body — this version cannot include the request
    /// payload in the report.
    /// </summary>
    [Obsolete("Use HandleExceptionAsync(ex, statusCode, context, requestBody, ct) so the body is captured per-request. This overload will be removed in v3.0.0.", error: false)]
    public Task HandleExceptionAsync(Exception ex, int statusCode, HttpContext context, CancellationToken cancellationToken = default)
        => HandleExceptionAsync(ex, statusCode, context, (JObject?)null, cancellationToken);

    private static RequestData CollectRequestData(HttpContext context, Exception exception, JObject? requestBody)
    {
        context.Request.EnableBuffering();

        var requestData = new RequestData
        {
            Method = context.Request.Method,
            Path = context.Request.Path,
            Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            QueryString = context.Request.QueryString.ToString(),
            ExceptionDetails = GetFullExceptionDetails(exception).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None),
            Body = requestBody
        };

        return requestData;
    }

    private static async Task<JObject?> ReadRequestBodyAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Body.CanRead)
            return null;

        request.EnableBuffering();
        request.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
#if NET7_0_OR_GREATER
        var body = await reader.ReadToEndAsync(cancellationToken);
#else
        var body = await reader.ReadToEndAsync();
#endif

        request.Body.Seek(0, SeekOrigin.Begin); // rewind again

        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            return JsonConvert.DeserializeObject<JObject>(body) ?? new JObject();
        }
        catch
        {
            // fallback: raw string if not JSON
            return new JObject { ["raw"] = body };
        }
    }

    private static string GetFullExceptionDetails(Exception ex)
    {
        if (ex == null) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("🔥 Exception Details:");

        CollectExceptionDetails(ex, sb, 0);

        return sb.ToString();
    }

    private static void CollectExceptionDetails(Exception ex, StringBuilder sb, int level)
    {
        if (ex == null) return;

        string indent = new string(' ', level * 4); // Indent inner exceptions
        sb.AppendLine($"{indent}📌 Message: {ex.Message}");
        sb.AppendLine($"{indent}🔍 Type: {ex.GetType().FullName}");
        sb.AppendLine($"{indent}📍 StackTrace: {ex.StackTrace}");

        // Handle AggregateException separately (for Task and Parallel exceptions)
        if (ex is AggregateException aggEx)
        {
            foreach (var inner in aggEx.InnerExceptions)
            {
                sb.AppendLine($"{indent}🔄 Aggregate Inner Exception:");
                CollectExceptionDetails(inner, sb, level + 1);
            }
        }
        else if (ex.InnerException != null)
        {
            sb.AppendLine($"{indent}➡ Inner Exception:");
            CollectExceptionDetails(ex.InnerException, sb, level + 1);
        }
    }
}
