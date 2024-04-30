namespace GenericTableAPI.Models
{
    public class StringResponse(int code, string content)
    {
        public int Code { get; } = code;
        public string? Content { get; } = content;
    }
}
