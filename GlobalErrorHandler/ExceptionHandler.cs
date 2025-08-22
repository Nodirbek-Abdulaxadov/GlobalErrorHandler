namespace GlobalErrorHandler;

public static class ExceptionHandler
{
    private static readonly Dictionary<Type, int> _exceptionMappings = new();

    /// <summary>
    /// Add custom exception mapping
    /// </summary>
    public static void Register<TException>(HttpStatusCode statusCode) where TException : Exception
    {
        _exceptionMappings[typeof(TException)] = (int)statusCode;
    }

    /// <summary>
    /// Add custom exception mapping
    /// </summary>
    public static void Register<TException>(int statusCode) where TException : Exception
    {
        _exceptionMappings[typeof(TException)] = statusCode;
    }

    /// <summary>
    /// Try to get status code for given exception
    /// </summary>
    public static bool TryGetStatusCode(Exception ex, out int statusCode)
    {
        if (_exceptionMappings.TryGetValue(ex.GetType(), out statusCode))
            return true;

        statusCode = 500; // default InternalServerError
        return false;
    }
}