using System.Text;
using GlobalErrorHandler;
using GlobalErrorHandler.Models;
using LoggerBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;

namespace GlobalErrorHandler.LoggerBotIntegration;

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

public static class LoggerBotSinkServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="LoggerBotErrorHandlerSink"/> as the active
    /// <see cref="IErrorHandlerSink"/>. Make sure <c>ILoggerService</c> from the
    /// <c>LoggerBot</c> package is also registered in DI.
    /// </summary>
    public static IServiceCollection AddLoggerBotSink(this IServiceCollection services)
    {
        services.AddGlobalErrorHandler();
        services.Replace(ServiceDescriptor.Singleton<IErrorHandlerSink, LoggerBotErrorHandlerSink>());
        return services;
    }
}
