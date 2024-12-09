using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RestSharp;
using System.Net;
using RestSharp.Authenticators;

public class BaseTestClass
{
    protected RestClient _client;
    protected static string _bearerToken;

    private const string BaseUrl = "http://localhost:5066/";
    

    protected string JWTAuthUsername = "admin";
    protected string JWTAuthPassword = "passwd";

    protected string BasicAuthUsername = "admin";
    protected string BasicAuthPassword = "passwd";

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
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(_client, request);
        RestResponse response = _client.Execute(request);
        if (HttpStatusCode.OK != response.StatusCode)
            throw new Exception($"Got response code: {response.StatusCode} from GET request!");

        return (int)JsonConvert.DeserializeObject<IEnumerable<JToken>>(response.Content).First().First.First;
    }
}
