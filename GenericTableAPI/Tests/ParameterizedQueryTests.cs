using GenericTableAPI.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace GenericTableAPI.Tests
{
    [TestClass]
    public class ParameterizedQueryTests
    {
        private const string SqlServerConnectionString = "Server=localhost;Database=TestDB;User Id=SA;Password=Password123;TrustServerCertificate=True";
        private const string OracleConnectionString = "DATA SOURCE=localhost:1521/XE;USER ID=test;PASSWORD=password";

        [TestMethod]
        public void TestGetByIdQueryParameterized_SqlServer()
        {
            // Arrange
            string tableName = "Users";
            string schemaName = "dbo";
            string primaryKeyColumn = "Id";

            // Act
            var (query, parameters) = SyntaxService.GetByIdQueryParameterized(tableName, schemaName, primaryKeyColumn, SqlServerConnectionString);

            // Assert
            Assert.AreEqual("SELECT * FROM dbo.Users WHERE Id = @Id", query);
            Assert.IsNotNull(parameters);
        }

        [TestMethod]
        public void TestGetByIdQueryParameterized_Oracle()
        {
            // Arrange
            string tableName = "Users";
            string schemaName = "TEST_SCHEMA";
            string primaryKeyColumn = "Id";

            // Act
            var (query, parameters) = SyntaxService.GetByIdQueryParameterized(tableName, schemaName, primaryKeyColumn, OracleConnectionString);

            // Assert
            Assert.AreEqual("SELECT * FROM TEST_SCHEMA.Users WHERE Id = :Id", query);
            Assert.IsNotNull(parameters);
        }

        [TestMethod]
        public void TestAddQueryParameterized_SqlServer()
        {
            // Arrange
            string tableName = "Users";
            string schemaName = "dbo";
            string primaryKeyColumn = "Id";
            var values = new Dictionary<string, object?>
            {
                ["Name"] = "John Doe",
                ["Email"] = "john@example.com",
                ["Age"] = 30
            };

            // Act
            var (query, parameters) = SyntaxService.AddQueryParameterized(tableName, schemaName, values, primaryKeyColumn, SqlServerConnectionString);

            // Assert
            Assert.AreEqual("INSERT INTO dbo.Users (Name, Email, Age) OUTPUT Inserted.Id VALUES (@Name, @Email, @Age)", query);
            Assert.IsNotNull(parameters);
            Assert.AreEqual(3, ((Dictionary<string, object?>)parameters).Count);
        }

        [TestMethod]
        public void TestAddQueryParameterized_Oracle()
        {
            // Arrange
            string tableName = "Users";
            string schemaName = "TEST_SCHEMA";
            string primaryKeyColumn = "Id";
            var values = new Dictionary<string, object?>
            {
                ["Name"] = "John Doe",
                ["Email"] = "john@example.com",
                ["Age"] = 30
            };

            // Act
            var (query, parameters) = SyntaxService.AddQueryParameterized(tableName, schemaName, values, primaryKeyColumn, OracleConnectionString);

            // Assert
            Assert.AreEqual("INSERT INTO TEST_SCHEMA.Users (Name, Email, Age) VALUES (:Name, :Email, :Age) RETURNING Id INTO :ret", query);
            Assert.IsNotNull(parameters);
            Assert.AreEqual(3, ((Dictionary<string, object?>)parameters).Count);
        }

        [TestMethod]
        public void TestUpdateQueryParameterized_SqlServer()
        {
            // Arrange
            string tableName = "Users";
            string schemaName = "dbo";
            string primaryKeyColumn = "Id";
            var values = new Dictionary<string, object?>
            {
                ["Name"] = "Jane Doe",
                ["Email"] = "jane@example.com"
            };

            // Act
            var (query, parameters) = SyntaxService.UpdateQueryParameterized(tableName, schemaName, values, primaryKeyColumn, SqlServerConnectionString);

            // Assert
            Assert.AreEqual("UPDATE dbo.Users SET Name = @Name, Email = @Email WHERE Id = @Id", query);
            Assert.IsNotNull(parameters);
            Assert.AreEqual(2, ((Dictionary<string, object?>)parameters).Count);
        }

        [TestMethod]
        public void TestUpdateQueryParameterized_Oracle()
        {
            // Arrange
            string tableName = "Users";
            string schemaName = "TEST_SCHEMA";
            string primaryKeyColumn = "Id";
            var values = new Dictionary<string, object?>
            {
                ["Name"] = "Jane Doe",
                ["Email"] = "jane@example.com"
            };

            // Act
            var (query, parameters) = SyntaxService.UpdateQueryParameterized(tableName, schemaName, values, primaryKeyColumn, OracleConnectionString);

            // Assert
            Assert.AreEqual("UPDATE TEST_SCHEMA.Users SET Name = :Name, Email = :Email WHERE Id = :Id", query);
            Assert.IsNotNull(parameters);
            Assert.AreEqual(2, ((Dictionary<string, object?>)parameters).Count);
        }

        [TestMethod]
        public void TestDeleteQueryParameterized_SqlServer()
        {
            // Arrange
            string tableName = "Users";
            string schemaName = "dbo";
            string primaryKeyColumn = "Id";

            // Act
            var (query, parameters) = SyntaxService.DeleteQueryParameterized(tableName, schemaName, primaryKeyColumn, SqlServerConnectionString);

            // Assert
            Assert.AreEqual("DELETE FROM dbo.Users WHERE Id = @Id", query);
            Assert.IsNotNull(parameters);
        }

        [TestMethod]
        public void TestDeleteQueryParameterized_Oracle()
        {
            // Arrange
            string tableName = "Users";
            string schemaName = "TEST_SCHEMA";
            string primaryKeyColumn = "Id";

            // Act
            var (query, parameters) = SyntaxService.DeleteQueryParameterized(tableName, schemaName, primaryKeyColumn, OracleConnectionString);

            // Assert
            Assert.AreEqual("DELETE FROM TEST_SCHEMA.Users WHERE Id = :Id", query);
            Assert.IsNotNull(parameters);
        }

        [TestMethod]
        public void TestSqlInjectionPrevention()
        {
            // Arrange
            string tableName = "Users";
            string schemaName = "dbo";
            string primaryKeyColumn = "Id";
            var maliciousValues = new Dictionary<string, object?>
            {
                ["Name"] = "'; DROP TABLE Users; --",
                ["Email"] = "'; DELETE FROM Users; --",
                ["Age"] = "'; UPDATE Users SET Name = 'Hacked'; --"
            };

            // Act
            var (query, parameters) = SyntaxService.AddQueryParameterized(tableName, schemaName, maliciousValues, primaryKeyColumn, SqlServerConnectionString);

            // Assert
            // The query should use parameterized values, not string concatenation
            Assert.IsTrue(query.Contains("@Name") && query.Contains("@Email") && query.Contains("@Age"));
            Assert.IsFalse(query.Contains("'; DROP TABLE Users; --"));
            Assert.IsFalse(query.Contains("'; DELETE FROM Users; --"));
            Assert.IsFalse(query.Contains("'; UPDATE Users SET Name = 'Hacked'; --"));

            // The malicious values should be in the parameters, not in the query string
            var paramDict = (Dictionary<string, object?>)parameters;
            Assert.AreEqual("'; DROP TABLE Users; --", paramDict["Name"]);
            Assert.AreEqual("'; DELETE FROM Users; --", paramDict["Email"]);
            Assert.AreEqual("'; UPDATE Users SET Name = 'Hacked'; --", paramDict["Age"]);
        }
    }
}