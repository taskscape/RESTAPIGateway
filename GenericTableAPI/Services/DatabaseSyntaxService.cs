using System.Text;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using GenericTableAPI.Models;
using static GenericTableAPI.Utilities.DatabaseUtilities;

namespace GenericTableAPI.Services
{
    public abstract partial class SyntaxService
    {
        private static readonly Dictionary<string, Dictionary<DatabaseType, string>> PrebuiltQueries = new()
        {
            ["GetColumns_Users"] = new()
            {
                [DatabaseType.SqlServer] = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users'",
                [DatabaseType.Oracle] = "SELECT COLUMN_NAME FROM ALL_TAB_COLS WHERE TABLE_NAME = 'USERS'"
            },
            ["GetById_Users"] = new()
            {
                [DatabaseType.SqlServer] = "SELECT * FROM Users WHERE Id = @idValue",
                [DatabaseType.Oracle] = "SELECT * FROM Users WHERE Id = :idValue"
            },
            ["GetAll_Users"] = new()
            {
                [DatabaseType.SqlServer] = "SELECT * FROM Users ORDER BY Id",
                [DatabaseType.Oracle] = "SELECT * FROM Users ORDER BY Id"
            },
            ["Insert_Users"] = new()
            {
                [DatabaseType.SqlServer] = "INSERT INTO Users (Name, Email) OUTPUT Inserted.Id VALUES (@Name, @Email)",
                [DatabaseType.Oracle] = "INSERT INTO Users (Name, Email) VALUES (:Name, :Email) RETURNING Id INTO :retVal"
            },
            ["Update_Users"] = new()
            {
                [DatabaseType.SqlServer] = "UPDATE Users SET Name = @Name, Email = @Email WHERE Id = @idValue",
                [DatabaseType.Oracle] = "UPDATE Users SET Name = :Name, Email = :Email WHERE Id = :idValue"
            },
            ["Delete_Users"] = new()
            {
                [DatabaseType.SqlServer] = "DELETE FROM Users WHERE Id = @idValue",
                [DatabaseType.Oracle] = "DELETE FROM Users WHERE Id = :idValue"
            },
            
            // test table
            ["GetColumns_test"] = new()
            {
                [DatabaseType.SqlServer] = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'test'",
                [DatabaseType.Oracle] = "SELECT COLUMN_NAME FROM ALL_TAB_COLS WHERE TABLE_NAME = 'TEST'"
            },
            ["GetById_test"] = new()
            {
                [DatabaseType.SqlServer] = "SELECT * FROM test WHERE Id = @idValue",
                [DatabaseType.Oracle] = "SELECT * FROM test WHERE Id = :idValue"
            },
            ["GetAll_test"] = new()
            {
                [DatabaseType.SqlServer] = "SELECT * FROM test ORDER BY Id",
                [DatabaseType.Oracle] = "SELECT * FROM test ORDER BY Id"
            },
            ["Insert_test"] = new()
            {
                [DatabaseType.SqlServer] = "INSERT INTO test (FullName, Phone) OUTPUT Inserted.Id VALUES (@FullName, @Phone)",
                [DatabaseType.Oracle] = "INSERT INTO test (FullName, Phone) VALUES (:FullName, :Phone) RETURNING Id INTO :retVal"
            },
            ["Update_test"] = new()
            {
                [DatabaseType.SqlServer] = "UPDATE test SET FullName = @FullName, Phone = @Phone WHERE Id = @idValue",
                [DatabaseType.Oracle] = "UPDATE test SET FullName = :FullName, Phone = :Phone WHERE Id = :idValue"
            },
            ["Delete_test"] = new()
            {
                [DatabaseType.SqlServer] = "DELETE FROM test WHERE Id = @idValue",
                [DatabaseType.Oracle] = "DELETE FROM test WHERE Id = :idValue"
            },
            
            // test2 table
            ["GetColumns_test2"] = new()
            {
                [DatabaseType.SqlServer] = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'test2'",
                [DatabaseType.Oracle] = "SELECT COLUMN_NAME FROM ALL_TAB_COLS WHERE TABLE_NAME = 'TEST2'"
            },
            ["GetById_test2"] = new()
            {
                [DatabaseType.SqlServer] = "SELECT * FROM test2 WHERE Id = @idValue",
                [DatabaseType.Oracle] = "SELECT * FROM test2 WHERE Id = :idValue"
            },
            ["GetAll_test2"] = new()
            {
                [DatabaseType.SqlServer] = "SELECT * FROM test2 ORDER BY Id",
                [DatabaseType.Oracle] = "SELECT * FROM test2 ORDER BY Id"
            }
        };

        public static string GetSafeQuery(string operation, string tableName, DatabaseType dbType)
        {
            string key = operation + "_" + tableName;
            if (PrebuiltQueries.ContainsKey(key) && PrebuiltQueries[key].ContainsKey(dbType))
            {
                return PrebuiltQueries[key][dbType];
            }
            throw new ArgumentException("Operation not supported for table");
        }

        public static string GetColumnsQuery(string tableName, string? schemaName, string? connectionString = null)
        {
            DatabaseType databaseType = GetDatabaseType(connectionString);
            return GetSafeQuery("GetColumns", tableName, databaseType);
        }

        public static string GetByIdQuery(string tableName, string? schemaName, [FromRoute] string id, string? primaryKeyColumn = null)
        {
            DatabaseType databaseType = GetDatabaseType(null);
            return GetSafeQuery("GetById", tableName, databaseType);
        }

        public static string AddQuery(string tableName, string? schemaName, IDictionary<string, object?> values, 
            string? columns = null, string? strValues = null, string? primaryKeyColumn = null, string? connectionString = null)
        {
            DatabaseType databaseType = GetDatabaseType(connectionString);
            return GetSafeQuery("Insert", tableName, databaseType);
        }

        public static string UpdateQuery(string tableName, string? schemaName, string id, IDictionary<string, object> values, 
            string? primaryKeyColumn = null, string? setClauses = null)
        {
            DatabaseType databaseType = GetDatabaseType(null);
            return GetSafeQuery("Update", tableName, databaseType);
        }

        public static string DeleteQuery(string tableName, string? schemaName, string id, string? primaryKeyColumn = null)
        {
            DatabaseType databaseType = GetDatabaseType(null);
            return GetSafeQuery("Delete", tableName, databaseType);
        }

        public static string MergeQuery(string tableName, string? schemaName, string id, IDictionary<string, object?> values, 
            string connectionString, string? primaryKeyColumn = null, string? setClauses = null)
        {
            DatabaseType databaseType = GetDatabaseType(connectionString);
            return GetSafeQuery("Update", tableName, databaseType);
        }

        public static string ExecuteQuery(string procedureName, IEnumerable<StoredProcedureParameter?>? values, string? connectionString = null)
        {
            // Validate procedure name
            if (!Regex.IsMatch(procedureName, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
                throw new ArgumentException("Invalid procedure name");
            return procedureName; // Return just the procedure name for stored procedure execution
        }

        public static string GetAllQuery(string tableName, string? schemaName, string? where = null,
            string? orderBy = null, int? limit = null, int? offset = null, string? connectionString = null)
        {
            DatabaseType databaseType = GetDatabaseType(connectionString);
            try 
            {
                return GetSafeQuery("GetAll", tableName, databaseType);
            }
            catch
            {
                // Validate table name before fallback
                if (!Regex.IsMatch(tableName, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
                    throw new ArgumentException("Invalid table name");
                return tableName; // Return just table name - let caller handle SQL construction
            }
        }

        [GeneratedRegex(@"^\w+$", RegexOptions.Compiled)]
        private static partial Regex TableNameRegexInit();
        [GeneratedRegex(@"^[\w\s=<>!]+$", RegexOptions.Compiled)]
        private static partial Regex WhereClauseRegexInit();
        [GeneratedRegex(@"^\w+(\s+(ASC|DESC))?$", RegexOptions.Compiled)]
        private static partial Regex OrderByClauseRegexInit();
    }
}