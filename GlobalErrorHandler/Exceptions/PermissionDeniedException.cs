namespace GlobalErrorHandler.Exceptions;

public class PermissionDeniedException : Exception
{
    public PermissionDeniedException(string errorMessage = "You have no access") : base(errorMessage)
    {
        
    }
}