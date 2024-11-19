namespace GenericTableAPI.Models
{
    public class CompositeRequest(List<ApiRequest>? requests, object? response = null, bool debug = false)
    {
        public bool Debug { get; } = debug;
        public List<ApiRequest>? Requests { get; } = requests;
        public object? Response { get; } = response;
    }
}
