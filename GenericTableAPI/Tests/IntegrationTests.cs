using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

[TestClass]
public class IntegrationTests : IDisposable
{
    private RestClient _client;
    private const string BaseUrl = "https://localhost:7104/";
    private static string _bearerToken;

    [TestInitialize]
    public void Setup()
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
        request.AddJsonBody(new { userName = "foo", password = "123" });

        RestResponse response = _client.Execute(request);

        return JsonConvert.DeserializeObject<string>(response.Content);
    }
    //Returns the ID of the first element from Table 'test'
    private int GetFirstId()
    {
        //Get the ID of first element
        RestRequest request = new("api/test");
        request.AddHeader("Authorization", "Bearer " + _bearerToken);
        RestResponse response = _client.Execute(request);
        if (HttpStatusCode.OK != response.StatusCode)
            throw new Exception($"Got response code: {response.StatusCode} from GET request!");

        return (int)JsonConvert.DeserializeObject<IEnumerable<JToken>>(response.Content).First().First.First;
    }

    [TestMethod]
    public void Token_Post_ReturnsBadRequest()
    {
        // Arrange
        RestRequest request = new("api/Token", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddJsonBody(new { userName = "", Password = "" });

        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [TestMethod]
    public void Token_Post_ReturnsSuccess()
    {
        // Arrange
        RestRequest request = new("api/Token", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddJsonBody(new { userName = "foo", Password = "123" });

        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Get_ReturnsUnauthorized()
    {
        // Arrange
        RestRequest request = new("api/test");

        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [TestMethod]
    public void Test_Get_ReturnsSuccess()
    {
        // Arrange
        RestRequest request = new("api/test");
        request.AddHeader("Authorization", "Bearer "+ _bearerToken);
        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public void Test_GetWhere_ReturnsSuccess()
    {
        // Arrange                          WHERE Phone = '123' ORDER BY FULLNAME   LIMIT=10
        RestRequest request = new("api/test?where=Phone%3D%27123%27&orderBy=Fullname&limit=10");
        request.AddHeader("Authorization", "Bearer " + _bearerToken);
        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public void Test_Post_ReturnsSuccess()
    {
        // Arrange
        RestRequest request = new("api/test", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddJsonBody(new { FullName = "foo", Phone = "123" });
        request.AddHeader("Authorization", "Bearer " + _bearerToken);

        // Act
        RestResponse response = _client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_GetById_ReturnsSuccess()
    {
        int id = GetFirstId();
        // Arrange
        RestRequest request = new("api/test/{id}", Method.Get)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        request.AddHeader("Authorization", "Bearer " + _bearerToken);

        // Act
        RestResponse response = _client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Put_ReturnsSuccess()
    {
        var id = GetFirstId();
        // Arrange
        RestRequest request = new("api/test/{id}", Method.Put)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        request.AddJsonBody(new { FullName = "foo", Phone = "123" });
        request.AddHeader("Authorization", "Bearer " + _bearerToken);

        // Act
        RestResponse response = _client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Delete_ReturnsSuccess()
    {
        var id = GetFirstId();
        // Arrange
        RestRequest request = new("api/test/{id}", Method.Delete)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        request.AddHeader("Authorization", "Bearer " + _bearerToken);

        // Act
        RestResponse response = _client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    public void Dispose()
    {
        //throw new NotImplementedException();
    }
}