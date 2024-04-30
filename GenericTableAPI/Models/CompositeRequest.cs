namespace GenericTableAPI.Models
{
    public class CompositeRequest(List<ApiRequest>? requests)
    {
        public List<ApiRequest>? Requests { get; } = requests;
    }
}
