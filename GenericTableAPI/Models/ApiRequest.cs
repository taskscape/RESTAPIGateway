namespace GenericTableAPI.Models;

public class ApiRequest
{
    public string? Method { get; set; }
    public string? Endpoint { get; set; }
    public Dictionary<string, string>? Parameters { get; set; }
    public Dictionary<string, string>? Returns { get; set; }
}