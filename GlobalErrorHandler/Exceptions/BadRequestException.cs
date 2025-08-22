namespace GlobalErrorHandler.Exceptions;

public class BadRequestException : Exception
{
    public BadRequestException(string errorMessage = "Something went wrong") : base(errorMessage)
    {
    }
}