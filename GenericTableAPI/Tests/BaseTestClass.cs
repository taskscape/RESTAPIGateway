using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RestSharp;
using System.Net;

public class BaseTestClass
{
    protected RestClient _client;
    protected static string _bearerToken;

    private const string BaseUrl = "https://localhost:7104/";
    

    protected string JWTAuthUsername = "your_jwt_auth_username";
    protected string JWTAuthPassword = "your_jwt_auth_password";

    protected string BasicAuthUsername = "your_basic_auth_username";
    protected string BasicAuthPassword = "your_basic_auth_password";

    protected void Setup()
    {
        _client = new RestClient(BaseUrl);
        _bearerToken = GetBearerToken();
    }

    private string GetBearerToken()
    {
        RestRequest request = new("api/token", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddJsonBody(new { userName = JWTAuthUsername, password = JWTAuthPassword });

        RestResponse response = _client.Execute(request);

        return JsonConvert.DeserializeObject<string>(response.Content);
    }
    //Returns the ID of the first element from Table 'test'
    protected virtual int GetFirstId()
    {
        //Get the ID of first element
        RestRequest request = new("api/tables/test");
        request.AddHeader("Authorization", "Bearer " + _bearerToken);
        RestResponse response = _client.Execute(request);
        if (HttpStatusCode.OK != response.StatusCode)
            throw new Exception($"Got response code: {response.StatusCode} from GET request!");

        return (int)JsonConvert.DeserializeObject<IEnumerable<JToken>>(response.Content).First().First.First;
    }
}
