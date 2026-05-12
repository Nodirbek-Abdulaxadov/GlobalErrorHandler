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

services.AddGlobalErrorHandler();     // default = NullErrorHandlerSink (no Telegram)
services.AddLoggerBotSink();          // optional — opts into Telegram delivery via LoggerBot
app.UseErrorHandler();
```

## Customization

### Custom exception → status code mappings

```csharp
ExceptionHandler.Register<MyDomainException>(HttpStatusCode.Conflict);
```

Built-in defaults: `BadRequestException → 400`, `NotFoundException → 404`, `PermissionDeniedException → 403`.

### Send reports to Telegram (LoggerBot)

```csharp
using GlobalErrorHandler;

builder.Services.AddGlobalErrorHandler();
builder.Services.AddLoggerBotSink();
```

### Write your own sink

Implement `IErrorHandlerSink` and register it in DI (replacing the default `NullErrorHandlerSink`).

## Target frameworks

`net6.0; net7.0; net8.0; net9.0; net10.0`
