using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using Tests;

[TestClass]
public class IntegrationTests : BaseTestClass
{
    #region GET Tests
    [TestMethod]
    public void Test_Get_BasicAuth_Success()
    {
        // Arrange
        RestRequest request = new("/api/tables/test");
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        // Act
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Get_BasicAuth_WrongCredentials()
    {
        // Arrange
        RestRequest request = new("/api/tables/test");
        new HttpBasicAuthenticator("foo", "123").Authenticate(Client, request);
        // Act
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [TestMethod]
    public void Test_Get_BearerAuth_Success()
    {
        // Arrange
        RestRequest request = new("/api/tables/test");
        request.AddHeader("Authorization", "Bearer " + GetBearerToken());
        // Act
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Get_BearerAuth_WrongCredentials()
    {
        // Arrange
        RestRequest request = new("/api/tables/test");
        request.AddHeader("Authorization", "Bearer " + GetBearerToken("foo", "123"));
        // Act
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [TestMethod]
    public void Test_Get_ReturnsUnauthorized()
    {
        // Arrange
        RestRequest request = new("/api/tables/test");

        // Act
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [TestMethod]
    public void Test_Get_ReturnsSuccess()
    {
        // Arrange
        RestRequest request = new("/api/tables/test");
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        // Act
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public void Test_GetWhere_ReturnsSuccess()
    {
        // Arrange                          WHERE Phone = '123' ORDER BY FULLNAME   LIMIT=10
        RestRequest request = new("/api/tables/test?where=Phone%3D%27123%27&orderBy=Fullname&limit=10");
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        // Act
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_GetById_ReturnsSuccess()
    {
        int id = GetFirstId();
        // Arrange
        RestRequest request = new("/api/tables/test/{id}", Method.Get)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);

        // Act
        RestResponse response = Client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_GetById_ColumnName_ReturnsSuccess()
    {
        int id = GetFirstId();
        // Arrange
        RestRequest request = new("/api/tables/test/{id}?primaryKeyColumnName=id", Method.Get)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);

        // Act
        RestResponse response = Client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_GetById_ColumnName_ReturnsError()
    {
        int id = GetFirstId();
        // Arrange
        RestRequest request = new("/api/tables/test/{id}?primaryKeyColumnName=abc", Method.Get)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);

        // Act
        RestResponse response = Client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
    [TestMethod]
    public void Test_Get_TablePermissions_ReturnsNoContent()
    {
        // Arrange
        RestRequest request = new("/api/tables/testempty");
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        // Act
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
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
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
    //TODO: REIMPLEMENT
    //[TestMethod]
    //public void Token_Post_ReturnsSuccess()
    //{
    //    // Arrange
    //    RestRequest request = new("api/Token", Method.Post)
    //    {
    //        RequestFormat = DataFormat.Json
    //    };
    //    request.AddJsonBody(new { userName = JWTAuthUsername, Password = JWTAuthPassword });

    //    // Act
    //    RestResponse response = _client.Execute(request);

    //    // Assert
    //    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    //}
    [TestMethod]
    public void Test_Post_ReturnsSuccess()
    {
        // Arrange
        RestRequest request = new("/api/tables/test", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddJsonBody(new { FullName = "foo", Phone = "123" });
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);

        // Act
        RestResponse response = Client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    }
    [TestMethod]
    public void Test_Post_TablePermissions_ReturnsForbidden()
    {
        // Arrange
        RestRequest request = new("/api/tables/test2", Method.Post);
        request.AddJsonBody(new { FullName = "foo", Phone = "123" });
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        // Act
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
    #endregion

    #region PUT Tests
    [TestMethod]
    public void Test_Put_ReturnsSuccess()
    {
        int id = GetFirstId();
        // Arrange
        RestRequest request = new("/api/tables/test/{id}", Method.Put)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        request.AddJsonBody(new { FullName = "foo", Phone = "123" });
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);

        // Act
        RestResponse response = Client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Put_ColumnName_ReturnsSuccess()
    {
        int id = GetFirstId();
        // Arrange
        RestRequest request = new("/api/tables/test/{id}?primaryKeyColumnName=id", Method.Put)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        request.AddJsonBody(new { FullName = "foo", Phone = "123" });
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);

        // Act
        RestResponse response = Client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Put_ColumnName_ReturnsError()
    {
        int id = GetFirstId();
        // Arrange
        RestRequest request = new("/api/tables/test/{id}?primaryKeyColumnName=abc", Method.Put)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        request.AddJsonBody(new { FullName = "foo", Phone = "123" });
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);

        // Act
        RestResponse response = Client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
    #endregion

    #region DELETE Tests
    [TestMethod]
    public void Test_Delete_ReturnsSuccess()
    {
        int id = GetFirstId();
        // Arrange
        RestRequest request = new("/api/tables/test/{id}", Method.Delete)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);

        // Act
        RestResponse response = Client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Delete_ColumnName_ReturnsSuccess()
    {
        int id = GetFirstId();
        // Arrange
        RestRequest request = new("/api/tables/test/{id}?primaryKeyColumnName=id", Method.Delete)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);

        // Act
        RestResponse response = Client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Delete_ColumnName_ReturnsError()
    {
        int id = GetFirstId();
        // Arrange
        RestRequest request = new("/api/tables/test/{id}?primaryKeyColumnName=abc", Method.Delete)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddUrlSegment("id", id);
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);

        // Act
        RestResponse response = Client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
    #endregion
}