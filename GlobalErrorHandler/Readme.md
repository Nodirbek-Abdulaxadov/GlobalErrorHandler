# GlobalErrorHandler NuGet Package

## Overview

The GlobalErrorHandler NuGet package provides middleware and extension methods to handle and log exceptions globally in ASP.NET Core applications. It includes an `ErrorHandlerMiddleware` to catch exceptions and log them, and an extension method `UseErrorHandler` to easily integrate the middleware into the application pipeline.

## Installation

You can install the GlobalErrorHandler NuGet package via the NuGet Package Manager or the .NET CLI:

```bash

dotnet add package GlobalErrorHandler

```

## Usage
Register ErrorHandlerMiddleware
In your ASP.NET Core application's startup code, register the ErrorHandlerMiddleware by using the UseErrorHandler extension method:

```csharp

using GlobalErrorHandler;

public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        app.UseErrorHandler();
    }
}

```
