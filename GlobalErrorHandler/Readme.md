# GlobalErrorHandler NuGet Package

## Overview

The GlobalErrorHandler NuGet package provides middleware and extension methods to handle and log exceptions globally in ASP.NET Core applications. It includes an `ErrorHandlerMiddleware` to catch exceptions and an extension method `UseErrorHandler` to easily integrate the middleware into the application pipeline.

Reports are published through a pluggable `IErrorHandlerSink` so the core package no longer hard-depends on LoggerBot. Telegram support lives in the optional `GlobalErrorHandler.LoggerBot` package.

## Installation

```bash
dotnet add package GlobalErrorHandler
# Optional, for Telegram delivery via LoggerBot:
dotnet add package GlobalErrorHandler.LoggerBot
```

## Usage

```csharp
using GlobalErrorHandler;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGlobalErrorHandler();

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

Built-in defaults: `BadRequestException → 400`, `NotFoundException → 404`, `PermissionDeniedException → 403`.

### Send reports to Telegram (LoggerBot)

Add the optional package and call `AddLoggerBotSink()`:

```csharp
using GlobalErrorHandler;
using GlobalErrorHandler.LoggerBotIntegration;

builder.Services.AddGlobalErrorHandler();
builder.Services.AddLoggerBotSink();
```

### Write your own sink

Implement `IErrorHandlerSink` and register it in DI (replacing the default `NullErrorHandlerSink`).

## Target frameworks

`net6.0; net7.0; net8.0; net9.0; net10.0`
