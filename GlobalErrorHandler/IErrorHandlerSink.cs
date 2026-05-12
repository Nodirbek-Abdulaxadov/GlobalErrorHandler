using GlobalErrorHandler.Models;

namespace GlobalErrorHandler;

/// <summary>
/// Pluggable destination for error reports produced by <see cref="ErrorHandlerMiddleware"/>.
/// Implementations are resolved through DI — register one via <c>AddGlobalErrorHandler()</c>
/// (defaults to <see cref="NullErrorHandlerSink"/>) or call <c>AddLoggerBotSink()</c>
/// from the <c>GlobalErrorHandler.LoggerBot</c> package.
/// </summary>
public interface IErrorHandlerSink
{
    Task PublishAsync(
        string title,
        Exception exception,
        RequestData? request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default no-op sink. Used when no other <see cref="IErrorHandlerSink"/> is registered.
/// </summary>
public sealed class NullErrorHandlerSink : IErrorHandlerSink
{
    public Task PublishAsync(
        string title,
        Exception exception,
        RequestData? request,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
