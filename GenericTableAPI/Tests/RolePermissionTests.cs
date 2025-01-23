using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using Tests;

[TestClass]
public class RolePermissionTests : BaseTestClass
{
    //IMPORTANT!
    /*
    FOR THOSE TESTS, THE CONFIGURATION BELOW IS REQUIRED FOR THE AUTHENTICATION
    "BasicAuthSettings": [
    {
      "Username": "user1",
      "Password": "passwd"
    },
    {
      "Username": "user2",
      "Password": "passwd",
      "Role": "Role2"
    },
    {
      "Username": "user3",
      "Password": "passwd",
      "Role": "Role3"
    }

    TABLE CONFIGURATION:
    "*": {
        "select": [ "*" ],
        "insert": [ "rolename:Role2" ],
        "update": []
      },
    "test": {
        "select": [ "*" ],
        "update": [ "Role1", "rolename:Role2" ],
        "delete": [ "username:user3" ],
        "insert": [ "rolename:Role1", "Role2", "rolename:Role3" ]
      },
    "testnotfound": {
        "select": [ "username:user1", "Role3", "rolename:Role2" ]
      },
     */

    private readonly string[] _users = ["user1", "user2", "user3", "user4"];
    private readonly string _password = "passwd";

    [TestMethod]
    public void Test_NotFound_Success()
    {
        foreach (string? user in _users)
        {
            // Arrange
            RestRequest request = new("/api/tables/testnotfound");
            new HttpBasicAuthenticator(user, _password).Authenticate(Client, request);
            // Act
            RestResponse response = Client.Execute(request);

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, $"{user} got response: {response.StatusCode} ");
            Console.WriteLine($"[SUCCESS] {user} not found!");
        }
    }

    [TestMethod]
    public void Test_GetAll_Success()
    {
        foreach (string? user in _users)
        {
            // Arrange
            RestRequest request = new("/api/tables/test");
            new HttpBasicAuthenticator(user, _password).Authenticate(Client, request);
            // Act
            RestResponse response = Client.Execute(request);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{user} got response: {response.StatusCode} ");
            Console.WriteLine($"[SUCCESS] {user} success!");
        }
    }

    [TestMethod]
    public void Test_Post_Success()
    {
        string[] users = [_users[1], _users[2], _users[3]];
        foreach (string? user in users)
        {
            // Arrange
            RestRequest request = new("/api/tables/test", Method.Post)
            {
                RequestFormat = DataFormat.Json
            };
            request.AddJsonBody(new { FullName = user, Phone = "123" });
            new HttpBasicAuthenticator(user, _password).Authenticate(Client, request);

            // Act
            RestResponse response = Client.Execute(request);
            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, $"{user} got response: {response.StatusCode} ");
            Console.WriteLine($"[SUCCESS] {user} created!");
        }
    }

    [TestMethod]
    public void Test_Post_BasicBearer_Success()
    {
        //TEST THE BEARER AUTHENTICATION
        RestRequest request = new("/api/tables/test", Method.Post)
        {
            RequestFormat = DataFormat.Json
        };
        request.AddJsonBody(new { FullName = "test-bearer", Phone = "123" });
        request.AddHeader("Authorization", "Bearer " + GetBearerToken("test", _password));

        // Act
        RestResponse response = Client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, $"JWTAdmin got response: {response.StatusCode} ");
        Console.WriteLine($"[SUCCESS] [JWT] test created!");

        //TEST THE BASIC AUTHENTICATION
        request = new($"/api/tables/test/{GetFirstId()}", Method.Delete)
        {
            RequestFormat = DataFormat.Json
        };
        new HttpBasicAuthenticator("test", _password).Authenticate(Client, request);

        // Act
        response = Client.Execute(request);
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"admin got response: {response.StatusCode} ");
        Console.WriteLine($"[SUCCESS] [basic] test deleted!");
    }

    [TestMethod]
    public void Test_Post_Forbidden()
    {
        string[] users = [_users[0]];
        foreach (string? user in users)
        {
            // Arrange
            RestRequest request = new("/api/tables/test", Method.Post)
            {
                RequestFormat = DataFormat.Json
            };
            request.AddJsonBody(new { FullName = user, Phone = "123" });
            new HttpBasicAuthenticator(user, _password).Authenticate(Client, request);

            // Act
            RestResponse response = Client.Execute(request);
            // Assert
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, $"{user} got response: {response.StatusCode}");
            Console.WriteLine($"[SUCCESS] {user} forbidden!");
        }
    }

    [TestMethod]
    public void Test_Put_Success()
    {
        int firstId = GetFirstId();
        string[] users = [_users[1], _users[3]];
        foreach (string? user in users)
        {
            // Arrange
            RestRequest request = new($"/api/tables/test/{firstId}", Method.Put)
            {
                RequestFormat = DataFormat.Json
            };
            request.AddJsonBody(new { FullName = user, Phone = "123" });
            new HttpBasicAuthenticator(user, _password).Authenticate(Client, request);

            // Act
            RestResponse response = Client.Execute(request);
            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{user} got response: {response.StatusCode} ");
            Console.WriteLine($"[SUCCESS] {user} success!");
        }
    }

    [TestMethod]
    public void Test_Put_Forbidden()
    {
        int firstId = GetFirstId();
        string[] users = [_users[0], _users[2]];
        foreach (string? user in users)
        {
            // Arrange
            RestRequest request = new($"/api/tables/test/{firstId}", Method.Put)
            {
                RequestFormat = DataFormat.Json
            };
            request.AddJsonBody(new { FullName = user, Phone = "123" });
            new HttpBasicAuthenticator(user, _password).Authenticate(Client, request);

            // Act
            RestResponse response = Client.Execute(request);
            // Assert
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, $"{user} got response: {response.StatusCode} ");
            Console.WriteLine($"[SUCCESS] {user} forbidden!");
        }
    }

    [TestMethod]
    public void Test_Delete_Success()
    {
        int firstId = GetFirstId();
        string[] users = [_users[2]];
        foreach (string? user in users)
        {
            // Arrange
            RestRequest request = new($"/api/tables/test/{firstId}", Method.Delete);
            new HttpBasicAuthenticator(user, _password).Authenticate(Client, request);
            // Act
            RestResponse response = Client.Execute(request);
            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{user} got response: {response.StatusCode} ");
            Console.WriteLine($"[SUCCESS] {user} success!");
        }
    }

    [TestMethod]
    public void Test_Delete_Forbidden()
    {
        int firstId = GetFirstId();
        string[] users = [_users[0], _users[1], _users[3]];
        foreach (string? user in users)
        {
            // Arrange
            RestRequest request = new($"/api/tables/test/{firstId}", Method.Delete);
            new HttpBasicAuthenticator(user, _password).Authenticate(Client, request);
            // Act
            RestResponse response = Client.Execute(request);
            // Assert
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, $"{user} got response: {response.StatusCode} ");
            Console.WriteLine($"[SUCCESS] {user} forbidden!");
        }
    }

    [TestMethod]
    public void Test_Get_Default_Success()
    {
        foreach (string? user in _users)
        {
            // Arrange
            RestRequest request = new($"/api/tables/test2", Method.Get);
            new HttpBasicAuthenticator(user, _password).Authenticate(Client, request);
            // Act
            RestResponse response = Client.Execute(request);
            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{user} got response: {response.StatusCode} ");
            Console.WriteLine($"[SUCCESS] {user} success!");
        }
    }

    [TestMethod]
    public void Test_Post_Default_Success()
    {
        string[] users = [_users[1], _users[3]];
        foreach (string? user in users)
        {
            // Arrange
            RestRequest request = new($"/api/tables/test2", Method.Post)
            {
                RequestFormat = DataFormat.Json
            };
            request.AddJsonBody(new { FullName = user, Phone = "123" });
            new HttpBasicAuthenticator(user, _password).Authenticate(Client, request);

            // Act
            RestResponse response = Client.Execute(request);
            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, $"{user} got response: {response.StatusCode} ");
            Console.WriteLine($"[SUCCESS] {user} created!");
        }
    }

    [TestMethod]
    public void Test_Post_Default_Forbidden()
    {
        string[] users = [_users[0], _users[2]];
        foreach (string? user in users)
        {
            // Arrange
            RestRequest request = new($"/api/tables/test2", Method.Post)
            {
                RequestFormat = DataFormat.Json
            };
            request.AddJsonBody(new { FullName = user, Phone = "123" });
            new HttpBasicAuthenticator(user, _password).Authenticate(Client, request);

            // Act
            RestResponse response = Client.Execute(request);
            // Assert
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, $"{user} got response: {response.StatusCode} ");
            Console.WriteLine($"[SUCCESS] {user} forbidden!");
        }
    }
    [TestMethod]
    public void Test_Put_Default_Forbidden()
    {
        int firstId = GetFirstId();
        foreach (string? user in _users)
        {
            // Arrange
            RestRequest request = new($"/api/tables/test2/{firstId}", Method.Put)
            {
                RequestFormat = DataFormat.Json
            };
            request.AddJsonBody(new { FullName = user, Phone = "123" });
            new HttpBasicAuthenticator(user, _password).Authenticate(Client, request);

            // Act
            RestResponse response = Client.Execute(request);
            // Assert
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, $"{user} got response: {response.StatusCode} ");
            Console.WriteLine($"[SUCCESS] {user} forbidden!");
        }
    }
    [TestMethod]
    public void Test_Delete_Default_Forbidden()
    {
        int firstId = GetFirstId();
        foreach (string? user in _users)
        {
            // Arrange
            RestRequest request = new($"/api/tables/test2/{firstId}", Method.Delete);
            new HttpBasicAuthenticator(user, _password).Authenticate(Client, request);
            // Act
            RestResponse response = Client.Execute(request);
            // Assert
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, $"{user} got response: {response.StatusCode} ");
            Console.WriteLine($"[SUCCESS] {user} forbidden!");
        }
    }



    override protected int GetFirstId()
    {
        //Get the ID of first element
        RestRequest request = new("api/tables/test");
        new HttpBasicAuthenticator(_users[0], _password).Authenticate(Client, request);
        RestResponse response = Client.Execute(request);
        if (HttpStatusCode.OK != response.StatusCode)
            throw new Exception($"Got response code: {response.StatusCode} from GET request!");

        return (int)JsonConvert.DeserializeObject<IEnumerable<JToken>>(response.Content).First().First.First;
    }
}
