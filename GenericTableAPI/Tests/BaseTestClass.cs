using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

namespace Tests;

public class BaseTestClass
{
    protected RestClient Client = new(BaseUrl);

    private const string BaseUrl = "http://localhost:5066/";


    private const string JWTAuthUsername = "admin";
    private const string JWTAuthPassword = "passwd";

    protected const string BasicAuthUsername = "admin";
    protected const string BasicAuthPassword = "passwd";

    protected string? GetBearerToken(string username, string password)
    {
        RestRequest request = new("api/token", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddJsonBody(new { userName = username, password = password });

        RestResponse? response = Client?.Execute(request);

        if (response?.Content == null) return null;
        return JsonConvert.DeserializeObject<string>(response.Content);
    }
    protected string? GetBearerToken()
    {
        return GetBearerToken(JWTAuthUsername, JWTAuthPassword);
    }
    //Returns the ID of the first element from Table 'test'
    protected virtual int GetFirstId()
    {
        //Get the ID of first element
        RestRequest request = new("api/tables/test");
        if (Client == null) return -1;
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        RestResponse response = Client.Execute(request);
        if (HttpStatusCode.OK != response.StatusCode)
            throw new Exception($"Got response code: {response.StatusCode} from GET request!");

        if (response.Content == null) return -1;
        JToken? firstFirst = (JsonConvert.DeserializeObject<IEnumerable<JToken>>(response.Content) ??
                              Array.Empty<JToken>()).First().First?.First;
        if (response.Content == null) return -1;
        if (firstFirst != null)
            return (int)firstFirst;

        return -1;
    }
}