using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;

[TestClass]
public class IntegrationTests : IDisposable
{
    private RestClient _client;
    private const string BaseUrl = "https://localhost:7104/";

    [TestInitialize]
    public void Setup()
    {
        _client = new RestClient(BaseUrl);
    }

    [TestMethod]
    public void Test_Get_ReturnsSuccess()
    {
        // Arrange
        RestRequest request = new("api/dapper/test");

        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public void Test_Post_ReturnsSuccess()
    {
        // Arrange
        RestRequest request = new("api/dapper/test", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddJsonBody(new { FullName = "foo", Phone = "123" });

        // Act
        RestResponse response = _client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_GetById_ReturnsSuccess()
    {
        var id = 10;
        // Arrange
        RestRequest request = new("api/dapper/test/{id}", Method.Get)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);

        // Act
        RestResponse response = _client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Put_ReturnsSuccess()
    {
        var id = 10;
        // Arrange
        RestRequest request = new("api/dapper/test/{id}", Method.Put)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        request.AddJsonBody(new { FullName = "foo", Phone = "123" });

        // Act
        RestResponse response = _client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Delete_ReturnsSuccess()
    {
        var id = 10;
        // Arrange
        RestRequest request = new("api/dapper/test/{id}", Method.Delete)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);

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