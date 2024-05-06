namespace GlobalErrorHandler.Models;

public class HttpResponseModel
{
    [JsonPropertyName("status")]
    public int? status { get; }

    [JsonPropertyName("message")]
    public string? message { get; }

    [JsonPropertyName("name")]
    public string? name { get; }

    public HttpResponseModel(int? status, string? message, string? name)
    {
        this.status = status;
        this.message = message;
        this.name = name;
    }
}