namespace GlobalErrorHandler.Models;

public class HttpResponseModel
{
    [JsonPropertyName("code")]
    public int? code { get; }

    [JsonPropertyName("status")]
    public string? status { get; }

    [JsonPropertyName("message")]
    public string? message { get; }

    public HttpResponseModel(int? code, string? message, string? status)
    {
        this.status = status;
        this.message = message;
        this.code = code;
    }
}