namespace GenericTableAPI.Models
{
    public class StringResponseModel
    {
        public int Code { get; set; }
        public string? Content { get; set; }

        public StringResponseModel(int code, string content)
        {
            Code = code;
            Content = content;
        }
    }
}
