namespace GenericTableAPI.Exceptions
{
    public class ResponseException : Exception
    {
        public int StatusCode { get; }
        public string? ErrorCode { get; }
        public object? AdditionalData { get; }

        public ResponseException(int statusCode, string? message = null, string? errorCode = null, object? additionalData = null)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            AdditionalData = additionalData;
        }
    }
}
