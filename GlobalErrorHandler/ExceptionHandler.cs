using System.Collections.Concurrent;
using GlobalErrorHandler.Exceptions;

namespace GlobalErrorHandler;

public static class ExceptionHandler
{
    private static readonly ConcurrentDictionary<Type, int> _exceptionMappings = new();

    static ExceptionHandler()
    {
        // Default mappings — can be overridden via Register<T>(...)
        _exceptionMappings[typeof(BadRequestException)] = 400;
        _exceptionMappings[typeof(NotFoundException)] = 404;
        _exceptionMappings[typeof(PermissionDeniedException)] = 403;
    }

    /// <summary>
    /// Add (or replace) custom exception mapping.
    /// </summary>
    public static void Register<TException>(HttpStatusCode statusCode) where TException : Exception
    {
        _exceptionMappings[typeof(TException)] = (int)statusCode;
    }

    /// <summary>
    /// Add (or replace) custom exception mapping.
    /// </summary>
    public static void Register<TException>(int statusCode) where TException : Exception
    {
        _exceptionMappings[typeof(TException)] = statusCode;
    }

    /// <summary>
    /// Try to get status code for given exception. Returns false (with 500 default) if not mapped.
    /// </summary>
    public static bool TryGetStatusCode(Exception ex, out int statusCode)
    {
        if (_exceptionMappings.TryGetValue(ex.GetType(), out statusCode))
            return true;

        statusCode = 500; // default InternalServerError
        return false;
    }
}
