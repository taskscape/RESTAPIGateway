namespace GenericTableAPI.Exceptions
{
    public class ResponseException : Exception
    {
        public int StatusCode { get; set; }

        public ResponseException(int statusCode, string? message = null) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
