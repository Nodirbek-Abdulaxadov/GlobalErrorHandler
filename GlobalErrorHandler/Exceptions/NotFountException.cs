namespace GlobalErrorHandler.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string errorMessage = "Item not found") : base(errorMessage)
    {
        
    }
}