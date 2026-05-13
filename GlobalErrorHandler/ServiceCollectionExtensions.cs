using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GlobalErrorHandler;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the GlobalErrorHandler middleware dependencies with a default no-op
    /// <see cref="IErrorHandlerSink"/>. Call <see cref="AddLoggerBotSink"/> to opt in
    /// to Telegram delivery via LoggerBot.
    /// </summary>
    public static IServiceCollection AddGlobalErrorHandler(this IServiceCollection services)
    {
        services.TryAddSingleton<IErrorHandlerSink, NullErrorHandlerSink>();
        return services;
    }

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
