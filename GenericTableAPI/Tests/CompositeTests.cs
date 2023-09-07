using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using System.Net;

[TestClass]
public class CompositeTests : BaseTestClass
{
    [TestInitialize]
    public void Init()
    {
        Setup();
    }

    [TestMethod]
    public void Test_GetAll_ReturnsSuccess()
    {
        // Arrange
        RestRequest request = new("api/composite", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddHeader("Authorization", "Bearer " + _bearerToken);
        string body = "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"get\",\r\n      \"endpoint\": \"https://localhost:7104/api/test\"\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_GetById_ReturnsSuccess()
    {
        int firstId = GetFirstId();
        // Arrange
        RestRequest request = new("api/composite", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddHeader("Authorization", "Bearer " + _bearerToken);
        string body = "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"get\",\r\n      \"endpoint\": \"https://localhost:7104/api/test/"+firstId+"\"\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_GetById_Parameter_ReturnsSuccess()
    {
        int firstId = GetFirstId();
        // Arrange
        RestRequest request = new("api/composite", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddHeader("Authorization", "Bearer " + _bearerToken);
        string body = "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"get\",\r\n      \"endpoint\": \"https://localhost:7104/api/test/{ID}\",\r\n      \"parameters\": {\r\n        \"ID\": \""+firstId+"\"\r\n      }\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Post_ReturnsSuccess()
    {
        // Arrange
        RestRequest request = new("api/composite", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddHeader("Authorization", "Bearer " + _bearerToken);
        string body = "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"post\",\r\n      \"endpoint\": \"https://localhost:7104/api/test\",\r\n      \"parameters\": {\r\n        \"phone\": \"109080\",\r\n        \"fullname\": \"composite-test\"\r\n      }\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Put_ReturnsSuccess()
    {
        int firstId = GetFirstId();
        // Arrange
        RestRequest request = new("api/composite", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddHeader("Authorization", "Bearer " + _bearerToken);
        string body = "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"put\",\r\n      \"endpoint\": \"https://localhost:7104/api/test/"+firstId+"\",\r\n      \"parameters\": {\r\n        \"phone\": \"109080\",\r\n        \"fullname\": \"composite-test-edit\"\r\n      }\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    public void Test_Delete_ReturnsSuccess()
    {
        int firstId = GetFirstId();
        // Arrange
        RestRequest request = new("api/composite", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddHeader("Authorization", "Bearer " + _bearerToken);
        string body = "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"delete\",\r\n      \"endpoint\": \"https://localhost:7104/api/test/" + firstId + "\",\r\n      \"parameters\": {\r\n        \"phone\": \"109080\",\r\n        \"fullname\": \"composite-test-edit\"\r\n      }\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    [TestMethod]
    //This Complex request contains 2 GET methods to gather data and store it as parameters, then a POST method with gathered data and finally a delete method.
    public void Test_ComplexRequest_ReturnsSuccess()
    {
        int firstId = GetFirstId();
        // Arrange
        RestRequest request = new("api/composite", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddHeader("Authorization", "Bearer " + _bearerToken);
        string body = "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"get\",\r\n      \"endpoint\": \"https://localhost:7104/api/test\",\r\n      \"returns\": {\r\n        \"aaa\": \"[11].Id\"\r\n      }\r\n    },\r\n    {\r\n      \"method\": \"get\",\r\n      \"endpoint\": \"https://localhost:7104/api/test/{aaa}\",\r\n      \"returns\": {\r\n        \"name\": \"FullName\",\r\n        \"phonenumber\": \"Phone\"\r\n      }\r\n    },\r\n    {\r\n      \"method\": \"post\",\r\n      \"endpoint\": \"https://localhost:7104/api/test\",\r\n      \"parameters\": {\r\n        \"fullname\": \"{name}\",\r\n        \"phone\": \"{phonenumber}\"\r\n      },\r\n      \"returns\": {\r\n        \"newID\": \"Id\"\r\n      }\r\n    },\r\n    {\r\n      \"method\": \"delete\",\r\n      \"endpoint\": \"https://localhost:7104/api/test/{newID}\"\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = _client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}

