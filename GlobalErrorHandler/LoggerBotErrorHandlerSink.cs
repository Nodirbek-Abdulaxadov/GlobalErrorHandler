using System.Text;
using GlobalErrorHandler.Models;
using LoggerBot.Services;
using Newtonsoft.Json;

namespace GlobalErrorHandler;

/// <summary>
/// <see cref="IErrorHandlerSink"/> that forwards reports to LoggerBot (Telegram).
/// Wraps the original v2.0.x behavior so existing consumers see no functional regression.
/// </summary>
public sealed class LoggerBotErrorHandlerSink : IErrorHandlerSink
{
    private readonly ILoggerService _loggerService;

    public LoggerBotErrorHandlerSink(ILoggerService loggerService)
    {
        _loggerService = loggerService;
    }

    public async Task PublishAsync(
        string title,
        Exception exception,
        RequestData? request,
        CancellationToken cancellationToken = default)
    {
        string data = request is null
            ? string.Empty
            : JsonConvert.SerializeObject(request, Formatting.Indented);
        byte[] payload = Encoding.UTF8.GetBytes(data);

        await _loggerService.ErrorAttachmentAsync(title, payload, null, cancellationToken);
    }
}
