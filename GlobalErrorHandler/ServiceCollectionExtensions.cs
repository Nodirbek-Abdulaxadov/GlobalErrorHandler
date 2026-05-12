using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GlobalErrorHandler;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the GlobalErrorHandler middleware dependencies with a default no-op
    /// <see cref="IErrorHandlerSink"/>. Call <c>AddLoggerBotSink()</c> from the
    /// <c>GlobalErrorHandler.LoggerBot</c> package to publish errors to Telegram.
    /// </summary>
    public static IServiceCollection AddGlobalErrorHandler(this IServiceCollection services)
    {
        services.TryAddSingleton<IErrorHandlerSink, NullErrorHandlerSink>();
        return services;
    }
}
