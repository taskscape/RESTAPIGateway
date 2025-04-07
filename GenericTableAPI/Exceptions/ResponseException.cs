namespace GenericTableAPI.Exceptions
{
    public class ResponseException : Exception
    {
        public int StatusCode { get; }
        public string? Responses { get; }

        public ResponseException(int statusCode, string? responses = null, string? message = null)
            : base(message)
        {
            StatusCode = statusCode;
            Responses = responses;
        }
    }
}
