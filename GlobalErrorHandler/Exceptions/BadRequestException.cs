namespace GlobalErrorHandler.Exceptions;

public class BadRequestException(string errorMessage = "Something went wrong")
    : Exception(errorMessage)
{ }