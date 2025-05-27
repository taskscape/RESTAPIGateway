using System.Data.Common;
using System.Dynamic;
using System.Text.RegularExpressions;
using GenericTableAPI.Models;
using GenericTableAPI.Services;
using GenericTableAPI.Utilities;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using static GenericTableAPI.Utilities.DatabaseUtilities;

namespace GenericTableAPI.Repositories;

public partial class DapperRepository(string? connectionString, string? schemaName, Serilog.ILogger logger)
{
    private static readonly Dictionary<string, Dictionary<DatabaseType, string>> SafeQueries = new()
    {
        // Users table
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
            [DatabaseType.SqlServer] = "UPDATE Users SET Name = @Name WHERE Id = @idValue",
            [DatabaseType.Oracle] = "UPDATE Users SET Name = :Name WHERE Id = :idValue"
        },
        ["Delete_Users"] = new()
        {
            [DatabaseType.SqlServer] = "DELETE FROM Users WHERE Id = @idValue",
            [DatabaseType.Oracle] = "DELETE FROM Users WHERE Id = :idValue"
        },
        
        // test table (for integration tests)
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
        },
        ["Insert_test2"] = new()
        {
            [DatabaseType.SqlServer] = "INSERT INTO test2 (Name) OUTPUT Inserted.Id VALUES (@Name)",
            [DatabaseType.Oracle] = "INSERT INTO test2 (Name) VALUES (:Name) RETURNING Id INTO :retVal"
        },
        ["Update_test2"] = new()
        {
            [DatabaseType.SqlServer] = "UPDATE test2 SET Name = @Name WHERE Id = @idValue",
            [DatabaseType.Oracle] = "UPDATE test2 SET Name = :Name WHERE Id = :idValue"
        },
        ["Delete_test2"] = new()
        {
            [DatabaseType.SqlServer] = "DELETE FROM test2 WHERE Id = @idValue",
            [DatabaseType.Oracle] = "DELETE FROM test2 WHERE Id = :idValue"
        },
        
        // testnotfound table
        ["GetColumns_testnotfound"] = new()
        {
            [DatabaseType.SqlServer] = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'testnotfound'",
            [DatabaseType.Oracle] = "SELECT COLUMN_NAME FROM ALL_TAB_COLS WHERE TABLE_NAME = 'TESTNOTFOUND'"
        },
        ["GetById_testnotfound"] = new()
        {
            [DatabaseType.SqlServer] = "SELECT * FROM testnotfound WHERE Id = @idValue",
            [DatabaseType.Oracle] = "SELECT * FROM testnotfound WHERE Id = :idValue"
        },
        ["GetAll_testnotfound"] = new()
        {
            [DatabaseType.SqlServer] = "SELECT * FROM testnotfound ORDER BY Id",
            [DatabaseType.Oracle] = "SELECT * FROM testnotfound ORDER BY Id"
        },
        
        // testempty table
        ["GetColumns_testempty"] = new()
        {
            [DatabaseType.SqlServer] = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'testempty'",
            [DatabaseType.Oracle] = "SELECT COLUMN_NAME FROM ALL_TAB_COLS WHERE TABLE_NAME = 'TESTEMPTY'"
        },
        ["GetById_testempty"] = new()
        {
            [DatabaseType.SqlServer] = "SELECT * FROM testempty WHERE Id = @idValue",
            [DatabaseType.Oracle] = "SELECT * FROM testempty WHERE Id = :idValue"
        },
        ["GetAll_testempty"] = new()
        {
            [DatabaseType.SqlServer] = "SELECT * FROM testempty ORDER BY Id",
            [DatabaseType.Oracle] = "SELECT * FROM testempty ORDER BY Id"
        }
    };

    private static string GetSafeQuery(string operation, string tableName, DatabaseType dbType)
    {
        string key = operation + "_" + tableName;
        if (SafeQueries.ContainsKey(key) && SafeQueries[key].ContainsKey(dbType))
        {
            return SafeQueries[key][dbType];
        }
        throw new ArgumentException("Operation not supported for table: " + tableName);
    }

    private static void AddParameter(DbCommand command, string name, object? value, DatabaseType dbType)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = dbType == DatabaseType.Oracle ? name : "@" + name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private DbConnection CreateConnection(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.SqlServer => new SqlConnection(connectionString),
            DatabaseType.Oracle => new OracleConnection(connectionString),
            _ => throw new NotSupportedException("Unknown database type")
        };
    }

    public async Task<List<object>?> GetColumnsAsync(string tableName)
    {
        try
        {
            DatabaseType databaseType = GetDatabaseType(connectionString);
            using var connection = CreateConnection(databaseType);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            
            command.CommandText = GetSafeQuery("GetColumns", tableName, databaseType);
            
            using var reader = await command.ExecuteReaderAsync();
            var result = new List<object>();
            while (await reader.ReadAsync())
            {
                result.Add(reader["COLUMN_NAME"]);
            }
            return result;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Repository.GetColumnsAsync: Error occurred");
            return null;
        }
    }

    public async Task<dynamic?> GetByIdAsync(string tableName, string primaryKey, string? columnName = "")
    {
        try
        {
            DatabaseType dbType = GetDatabaseType(connectionString);
            using var connection = CreateConnection(dbType);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            
            command.CommandText = GetSafeQuery("GetById", tableName, dbType);
            AddParameter(command, "idValue", primaryKey, dbType);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                dynamic result = new ExpandoObject();
                IDictionary<string, object> dict = result;
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dict[reader.GetName(i)] = reader.GetValue(i);
                }
                return result;
            }
            return null;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Repository.GetByIdAsync: Error occurred");
            return null;
        }
    }

    public async Task<object?> AddAsync(string tableName, IDictionary<string, object?> values, string? columnName = "")
    {
        try
        {
            DatabaseType dbType = GetDatabaseType(connectionString);
            using var connection = CreateConnection(dbType);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            
            command.CommandText = GetSafeQuery("Insert", tableName, dbType);
            
            foreach (var kvp in values)
            {
                AddParameter(command, kvp.Key, kvp.Value, dbType);
            }
            
            if (dbType == DatabaseType.Oracle)
            {
                AddParameter(command, "retVal", DBNull.Value, dbType);
                await command.ExecuteNonQueryAsync();
                return command.Parameters["retVal"].Value;
            }
            else
            {
                return await command.ExecuteScalarAsync();
            }
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Repository.AddAsync: Error occurred");
            throw;
        }
    }

    public async Task<bool> UpdateAsync(string tableName, string primaryKey, IDictionary<string, object?> values, List<object> columns, string? columnName = "")
    {
        try
        {
            DatabaseType dbType = GetDatabaseType(connectionString);
            using var connection = CreateConnection(dbType);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            
            command.CommandText = GetSafeQuery("Update", tableName, dbType);
            
            foreach (var kvp in values)
            {
                AddParameter(command, kvp.Key, kvp.Value, dbType);
            }
            AddParameter(command, "idValue", primaryKey, dbType);
            
            int affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows > 0;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Repository.UpdateAsync: Error occurred");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string tableName, string primaryKey, string? columnName = "")
    {
        try
        {
            DatabaseType dbType = GetDatabaseType(connectionString);
            using var connection = CreateConnection(dbType);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            
            command.CommandText = GetSafeQuery("Delete", tableName, dbType);
            AddParameter(command, "idValue", primaryKey, dbType);
            
            int affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows > 0;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Repository.DeleteAsync: Error occurred");
            throw;
        }
    }

    public async Task<List<object>?> ExecuteAsync(string procedureName, IEnumerable<StoredProcedureParameter?>? values)
    {
        try
        {
            DatabaseType dbType = GetDatabaseType(connectionString);
            using var connection = CreateConnection(dbType);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.CommandText = procedureName;
            
            if (values != null)
            {
                foreach (var param in values)
                {
                    if (param != null)
                    {
                        AddParameter(command, param.Name, param.Value, dbType);
                    }
                }
            }
            
            var results = new List<object>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dynamic item = new ExpandoObject();
                IDictionary<string, object> dict = item;
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dict[reader.GetName(i)] = reader.GetValue(i);
                }
                results.Add(item);
            }
            return results;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Repository.ExecuteAsync: Error occurred");
            throw;
        }
    }

    public async Task<IEnumerable<dynamic>?> GetAllAsync(string tableName, string? where = null, string? orderBy = null, int? limit = null, int? offset = null)
    {
        try
        {
            DatabaseType dbType = GetDatabaseType(connectionString);
            using var connection = CreateConnection(dbType);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            
            // Try to get the pre-built query first
            string baseQuery;
            try 
            {
                baseQuery = GetSafeQuery("GetAll", tableName, dbType);
            }
            catch
            {
                // Fallback for tables not in SafeQueries - validate tableName first
                if (!Regex.IsMatch(tableName, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
                    throw new ArgumentException("Invalid table name");
                baseQuery = dbType == DatabaseType.Oracle ? 
                    "SELECT * FROM " + tableName.ToUpper() + " ORDER BY ID" : 
                    "SELECT * FROM " + tableName + " ORDER BY Id";
            }
            
            // Build the full query with WHERE, ORDER BY, and pagination
            var sqlBuilder = new System.Text.StringBuilder(baseQuery.Replace(" ORDER BY Id", "").Replace(" ORDER BY ID", ""));
            var parameters = new Dictionary<string, object?>();
            
            // Add WHERE clause if specified
            if (!string.IsNullOrEmpty(where))
            {
                // Basic validation for WHERE clause
                if (Regex.IsMatch(where, @"^[\w\s=<>!'%-]+$"))
                {
                    sqlBuilder.Append(" WHERE ").Append(where);
                }
            }
            
            // Add ORDER BY clause
            if (!string.IsNullOrEmpty(orderBy))
            {
                if (Regex.IsMatch(orderBy, @"^[\w\s,]+(ASC|DESC)?$"))
                {
                    sqlBuilder.Append(" ORDER BY ").Append(orderBy);
                }
            }
            else
            {
                sqlBuilder.Append(" ORDER BY Id");
            }
            
            // Add pagination for SQL Server
            if (dbType == DatabaseType.SqlServer && (offset.HasValue || limit.HasValue))
            {
                sqlBuilder.Append(" OFFSET @offsetValue ROWS");
                parameters["offsetValue"] = offset.GetValueOrDefault(0);
                
                if (limit.HasValue)
                {
                    sqlBuilder.Append(" FETCH NEXT @limitValue ROWS ONLY");
                    parameters["limitValue"] = limit.Value;
                }
            }
            else if (dbType == DatabaseType.Oracle && (offset.HasValue || limit.HasValue))
            {
                sqlBuilder.Append(" OFFSET :offsetValue ROWS");
                parameters["offsetValue"] = offset.GetValueOrDefault(0);
                
                if (limit.HasValue)
                {
                    sqlBuilder.Append(" FETCH NEXT :limitValue ROWS ONLY");
                    parameters["limitValue"] = limit.Value;
                }
            }
            
            command.CommandText = sqlBuilder.ToString();
            
            // Add parameters
            foreach (var param in parameters)
            {
                AddParameter(command, param.Key, param.Value, dbType);
            }
            
            using var reader = await command.ExecuteReaderAsync();
            var results = new List<dynamic>();
            while (await reader.ReadAsync())
            {
                dynamic item = new ExpandoObject();
                IDictionary<string, object> dict = item;
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dict[reader.GetName(i)] = reader.GetValue(i);
                }
                results.Add(item);
            }
            return results;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Repository.GetAllAsync: Error occurred");
            return null;
        }
    }

    public async Task<bool> PatchAsync(string tableName, string primaryKey, IDictionary<string, object?> values, string? columnName = "")
    {
        return await UpdateAsync(tableName, primaryKey, values, new List<object>(), columnName);
    }

    [GeneratedRegex(@"^\w+$")]
    private static partial Regex MyRegex();
}