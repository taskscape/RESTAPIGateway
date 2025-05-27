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
            
            command.CommandText = SyntaxService.GetColumnsQuery(tableName, schemaName, connectionString);
            
            // Add table name parameter
            AddParameter(command, "tableNameParam", tableName, databaseType);
            
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
            
            // Get primary key column name if not provided
            string primaryKeyColumn = string.IsNullOrEmpty(columnName) 
                ? GetPrimaryKeyColumnName(connectionString, tableName, dbType) ?? "Id"
                : columnName;
            
            command.CommandText = SyntaxService.GetByIdQuery(tableName, schemaName, primaryKey, primaryKeyColumn);
            AddParameter(command, "idParam", primaryKey, dbType);
            
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
            
            // Get primary key column name if not provided
            string primaryKeyColumn = string.IsNullOrEmpty(columnName) 
                ? GetPrimaryKeyColumnName(connectionString, tableName, dbType) ?? "Id"
                : columnName;
            
            // Use the SyntaxService to generate parameterized query
            string query = SyntaxService.AddQuery(tableName, schemaName, values, null, null, primaryKeyColumn, connectionString);
            command.CommandText = query;
            
            // Add parameters for each value
            foreach (var kvp in values)
            {
                AddParameter(command, kvp.Key, kvp.Value, dbType);
            }
            
            if (dbType == DatabaseType.Oracle)
            {
                // Oracle requires a return parameter for RETURNING clause
                var returnParam = command.CreateParameter();
                returnParam.ParameterName = "retVal";
                returnParam.Direction = System.Data.ParameterDirection.Output;
                returnParam.DbType = System.Data.DbType.Int32;
                command.Parameters.Add(returnParam);
                
                await command.ExecuteNonQueryAsync();
                return returnParam.Value;
            }
            else
            {
                // SQL Server uses OUTPUT clause
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
            
            command.CommandText = SyntaxService.UpdateQuery(tableName, schemaName, primaryKey, values.Where(kvp => kvp.Value != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value!), string.IsNullOrEmpty(columnName) ? GetPrimaryKeyColumnName(connectionString, tableName, dbType) ?? "Id" : columnName);
            
            foreach (var kvp in values.Where(kvp => kvp.Value != null))
            {
                AddParameter(command, kvp.Key, kvp.Value, dbType);
            }
            AddParameter(command, "idParam", primaryKey, dbType);
            
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
            
            command.CommandText = SyntaxService.DeleteQuery(tableName, schemaName, primaryKey, string.IsNullOrEmpty(columnName) ? GetPrimaryKeyColumnName(connectionString, tableName, dbType) ?? "Id" : columnName);
            AddParameter(command, "idParam", primaryKey, dbType);
            
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
            
            // Use the SyntaxService to generate parameterized query
            string query = SyntaxService.GetAllQuery(tableName, schemaName, where, orderBy, limit, offset, connectionString);
            command.CommandText = query;
            
            // Add parameters for limit and offset if they exist
            if (limit.HasValue)
            {
                AddParameter(command, "limitParam", limit.Value, dbType);
            }
            if (offset.HasValue)
            {
                AddParameter(command, "offsetParam", offset.Value, dbType);
            }
            
            using var reader = await command.ExecuteReaderAsync();
            var result = new List<dynamic>();
            while (await reader.ReadAsync())
            {
                dynamic expandoObject = new ExpandoObject();
                IDictionary<string, object> dictionary = expandoObject;
                
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dictionary[reader.GetName(i)] = reader.GetValue(i);
                }
                result.Add(expandoObject);
            }
            return result;
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