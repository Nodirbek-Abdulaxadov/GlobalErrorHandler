﻿namespace GlobalErrorHandler.Models;

public class RequestData
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public IDictionary<string, string> Headers { get; set; } = null!;
    public string QueryString { get; set; } = string.Empty;
    public string? Body { get; set; }
}