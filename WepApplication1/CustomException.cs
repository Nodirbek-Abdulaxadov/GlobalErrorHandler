namespace WepApplication1;

public class CustomException : Exception
{
    public CustomException(string errorMessage = "Item not found") : base(errorMessage)
    {

    }
}