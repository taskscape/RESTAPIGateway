using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

[TestClass]
public class IntegrationTests : BaseTestClass
{
    [TestInitialize]
    public void Init()
    {
        Setup();
    }
    #region GET Tests
    [TestMethod]
    public void Test_Get_BasicAuth_Success()
    {
        // Arrange
        RestRequest request = new("api/test");
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(_client, request);
        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Get_BasicAuth_WrongCredentials()
    {
        // Arrange
        RestRequest request = new("api/test");
        new HttpBasicAuthenticator("foo", "123").Authenticate(_client, request);
        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
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
        request.AddHeader("Authorization", "Bearer " + _bearerToken);
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
    public void Test_GetById_ColumnName_ReturnsSuccess()
    {
        int id = GetFirstId();
        // Arrange
        RestRequest request = new("api/test/{id}?primaryKeyColumnName=id", Method.Get)
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
    public void Test_GetById_ColumnName_ReturnsError()
    {
        int id = GetFirstId();
        // Arrange
        RestRequest request = new("api/test/{id}?primaryKeyColumnName=abc", Method.Get)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        request.AddHeader("Authorization", "Bearer " + _bearerToken);

        // Act
        RestResponse response = _client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
    [TestMethod]
    public void Test_Get_TablePermissions_ReturnsNoContent()
    {
        // Arrange
        RestRequest request = new("api/test2");
        request.AddHeader("Authorization", "Bearer " + _bearerToken);
        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }
    #endregion

    #region POST Tests
    [TestMethod]
    public void Token_Post_ReturnsBadRequest()
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
        request.AddJsonBody(new { userName = JWTAuthUsername, Password = JWTAuthPassword });

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
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    }
    [TestMethod]
    public void Test_Post_TablePermissions_ReturnsForbidden()
    {
        // Arrange
        RestRequest request = new("api/test2", Method.Post);
        request.AddJsonBody(new { FullName = "foo", Phone = "123" });
        request.AddHeader("Authorization", "Bearer " + _bearerToken);
        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
    #endregion

    #region PUT Tests
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
    public void Test_Put_ColumnName_ReturnsSuccess()
    {
        var id = GetFirstId();
        // Arrange
        RestRequest request = new("api/test/{id}?primaryKeyColumnName=id", Method.Put)
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
    public void Test_Put_ColumnName_ReturnsError()
    {
        var id = GetFirstId();
        // Arrange
        RestRequest request = new("api/test/{id}?primaryKeyColumnName=abc", Method.Put)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        request.AddJsonBody(new { FullName = "foo", Phone = "123" });
        request.AddHeader("Authorization", "Bearer " + _bearerToken);

        // Act
        RestResponse response = _client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
    #endregion

    #region DELETE Tests
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
    [TestMethod]
    public void Test_Delete_ColumnName_ReturnsSuccess()
    {
        var id = GetFirstId();
        // Arrange
        RestRequest request = new("api/test/{id}?primaryKeyColumnName=id", Method.Delete)
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
    public void Test_Delete_ColumnName_ReturnsError()
    {
        var id = GetFirstId();
        // Arrange
        RestRequest request = new("api/test/{id}?primaryKeyColumnName=abc", Method.Delete)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        request.AddHeader("Authorization", "Bearer " + _bearerToken);

        // Act
        RestResponse response = _client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
    #endregion
}