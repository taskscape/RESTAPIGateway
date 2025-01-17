using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using Tests;

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
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        string body = "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"get\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test\"\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = Client.Execute(request);

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
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        string body = "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"get\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test/"+firstId+"\"\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = Client.Execute(request);

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
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        string body = "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"get\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test/{ID}\",\r\n      \"parameters\": {\r\n        \"ID\": \""+firstId+"\"\r\n      }\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = Client.Execute(request);

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
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        string body = "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"post\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test\",\r\n      \"parameters\": {\r\n        \"phone\": \"109080\",\r\n        \"fullname\": \"composite-test\"\r\n      }\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = Client.Execute(request);

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
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        string body =
            "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"put\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test/" +
            firstId +
            "\",\r\n      \"parameters\": {\r\n        \"phone\": \"109080\",\r\n        \"fullname\": \"composite-test-edit\"\r\n      }\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = Client.Execute(request);

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
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        string body =
            "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"delete\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test/" +
            firstId +
            "\",\r\n      \"parameters\": {\r\n        \"phone\": \"109080\",\r\n        \"fullname\": \"composite-test-edit\"\r\n      }\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    
    [TestMethod]
    //This Complex request contains 2 GET methods to gather data and store it as parameters, then a POST method with gathered data and finally a delete method.
    public void Test_ComplexRequest_ReturnsSuccess()
    {
        GetFirstId();
        // Arrange
        RestRequest request = new("api/composite", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        const string body =
            "{\r\n  \"debug\": true, \r\n  \"requests\": [\r\n    {\r\n      \"method\": \"get\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test\",\r\n      \"variables\": {\r\n        \"aaa\": \"[2].Id\"\r\n      }\r\n    },\r\n    {\r\n      \"method\": \"get\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test/{aaa}\",\r\n      \"variables\": {\r\n        \"name\": \"FullName\",\r\n        \"phonenumber\": \"Phone\"\r\n      }\r\n    },\r\n    {\r\n      \"method\": \"post\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test\",\r\n      \"parameters\": {\r\n        \"fullname\": \"{name}\",\r\n        \"phone\": \"{phonenumber}\"\r\n      },\r\n      \"variables\": {\r\n        \"newID\": \"Id\"\r\n      }\r\n    },\r\n    {\r\n      \"method\": \"delete\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test/{newID}\"\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public void Test_Foreach_Get_ReturnsSuccess()
    {
        // Arrange
        RestRequest request = new("api/composite", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        const string body =
            "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"GET\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test\",\r\n      \"variables\": {\r\n        \"testId\": \"[:].Id\"\r\n      }\r\n    },\r\n    {\r\n      \"method\": \"GET\",\r\n      \"foreach\": \"{testId}\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test/{testId}\",\r\n      \"variables\": {\r\n        \"alltask\": \"$\"\r\n      }\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    
    [TestMethod]
    //This Complex request contains 2 GET methods to gather data. (Second one is to simulate some foregin key gather). Then one POST request to create new data and DELETE to delete created data. All while using foreach feature
    public void Test_Foreach_Complex_ReturnsSuccess()
    {
        // Arrange
        RestRequest request = new("api/composite", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        const string body =
            "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"GET\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test\",\r\n      \"variables\": {\r\n        \"testId\": \"[-5:].Id\"\r\n      }\r\n    },\r\n    {\r\n      \"method\": \"GET\",\r\n      \"foreach\": \"{testId}\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test/{testId}\",\r\n      \"variables\": {\r\n        \"phones\": \"Phone\",\r\n        \"names\": \"Fullname\",\r\n        \"Ids\": \"Id\"\r\n      }\r\n    },\r\n    {\r\n      \"method\": \"POST\",\r\n      \"foreach\": \"{phones}\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test\",\r\n      \"parameters\": {\r\n        \"Fullname\": \"{phones}\",\r\n        \"Phone\": \"{phones}\"\r\n      },\r\n      \"variables\": {\r\n        \"createdIds\": \"Id\"\r\n      }\r\n    },\r\n    {\r\n      \"method\": \"DELETE\",\r\n      \"foreach\": \"{createdIds}\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test/{createdIds}\"\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    //The first request returns an int value. But the foreach normally expects an array. However even then it should work just fine.
    public void Test_Foreach_NotArray_ReturnsSuccess()
    {
        // Arrange
        RestRequest request = new("api/composite", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        new HttpBasicAuthenticator(BasicAuthUsername, BasicAuthPassword).Authenticate(Client, request);
        const string body =
            "{\r\n  \"requests\": [\r\n    {\r\n      \"method\": \"GET\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test\",\r\n      \"variables\": {\r\n        \"testId\": \"[-1:].Id\"\r\n      }\r\n    },\r\n    {\r\n      \"method\": \"GET\",\r\n      \"foreach\": \"{testId}\",\r\n      \"endpoint\": \"http://localhost:5066/api/tables/test/{testId}\",\r\n      \"variables\": {\r\n        \"names\": \"Fullname\"\r\n      }\r\n    }\r\n  ]\r\n}";
        request.AddBody(body, ContentType.Json);
        // Act
        RestResponse response = Client.Execute(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}

