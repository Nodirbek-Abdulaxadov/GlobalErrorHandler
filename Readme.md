# GlobalErrorHandler NuGet Package

## Overview

The GlobalErrorHandler NuGet package provides middleware and extension methods to handle and log exceptions globally in ASP.NET Core applications. It includes an `ErrorHandlerMiddleware` to catch exceptions and an extension method `UseErrorHandler` to easily integrate the middleware into the application pipeline.

Reports are published through a pluggable `IErrorHandlerSink`. By default a `NullErrorHandlerSink` is registered (no transport). Telegram delivery via LoggerBot is shipped in the same package and is opt-in through `AddLoggerBotSink()`.

## Installation

```bash
dotnet add package GlobalErrorHandler
```

## Usage

```csharp
using GlobalErrorHandler;

var builder = WebApplication.CreateBuilder(args);

services.AddGlobalErrorHandler();     // default = NullErrorHandlerSink (no Telegram)
services.AddLoggerBotSink();          // optional — opts into Telegram delivery via LoggerBot

var app = builder.Build();
app.UseErrorHandler();
app.MapControllers();
app.Run();
```

## Customization

### Custom exception → status code mappings

```csharp
ExceptionHandler.Register<MyDomainException>(HttpStatusCode.Conflict);
```

Built-in defaults: `BadRequestException → 400`, `NotFoundException → 404`, `PermissionDeniedException → 403`. Override them by calling `Register<T>(...)` again.

### Send reports to Telegram (LoggerBot)

`LoggerBot` itself still needs its own DI registration (`services.AddLoggerService(...)`).

```csharp
using GlobalErrorHandler;

builder.Services.AddGlobalErrorHandler();
builder.Services.AddLoggerBotSink(); // swaps the no-op sink for LoggerBot
```

### Write your own sink

Implement `IErrorHandlerSink` and register it in DI (replacing the default `NullErrorHandlerSink`).

```csharp
public sealed class MySerilogSink : IErrorHandlerSink
{
    public Task PublishAsync(string title, Exception ex, RequestData? request, CancellationToken ct)
    {
        // forward to Serilog, Seq, Elastic, etc.
        return Task.CompletedTask;
    }
}

builder.Services.AddGlobalErrorHandler();
builder.Services.Replace(ServiceDescriptor.Singleton<IErrorHandlerSink, MySerilogSink>());
```

## Target frameworks

`net6.0; net7.0; net8.0; net9.0; net10.0`
