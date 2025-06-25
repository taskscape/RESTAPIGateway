using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using static GenericTableAPI.Utilities.DatabaseUtilities;
using System.Data.Common;
using System.Data;
using Dapper;

namespace GenericTableAPI.Utilities;

public class DatabaseHandler : IDisposable
{
    private readonly DbConnection _connection;

    public DatabaseHandler(string? connectionString)
    {
        DatabaseType databaseType = GetDatabaseType(connectionString);
        _connection = databaseType switch
        {
            DatabaseType.SqlServer => new SqlConnection(connectionString),
            DatabaseType.Oracle => new OracleConnection(connectionString),
            _ => throw new NotSupportedException("Unsupported database type.")
        };
    }

    public void Open()
    {
        _connection.Open();
    }

    public void Close()
    {
        _connection.Close();
    }

    public Task<DbDataReader> ExecuteReaderAsync(string query)
    {
        DbCommand command = _connection.CreateCommand();
        command.CommandText = query;
        return command.ExecuteReaderAsync();
    }

    public Task<DbDataReader> ExecuteReaderAsync(string query, object? parameters)
    {
        return _connection.ExecuteReaderAsync(query, parameters);
    }

    public Task<object?> ExecuteScalarAsync(string query)
    {
        DbCommand command = _connection.CreateCommand();
        command.CommandText = query;
        return command.ExecuteScalarAsync();
    }

    public Task<object?> ExecuteScalarAsync(string query, object? parameters)
    {
        return _connection.ExecuteScalarAsync(query, parameters);
    }

    public Task<object?> ExecuteInsertAsync(string query)
    {
        if (_connection is OracleConnection)
            return OracleExecuteInsertAsync(query);
        else
            return ExecuteScalarAsync(query);
    }

    public Task<object?> ExecuteInsertAsync(string query, object? parameters)
    {
        if (_connection is OracleConnection)
            return OracleExecuteInsertAsync(query, parameters);
        else
            return ExecuteScalarAsync(query, parameters);
    }

    public IEnumerable<string> GetTableNames()
    {
        if (_connection is OracleConnection)
            return _connection.Query<string>("SELECT TABLE_NAME FROM USER_TABLES WHERE TABLE_NAME NOT LIKE 'MVIEW$%' AND TABLE_NAME NOT LIKE 'AQ$%' AND TABLE_NAME NOT LIKE 'OL$%' AND TABLE_NAME NOT IN ('SQLPLUS_PRODUCT_PROFILE', 'HELP', 'REDO_DB', 'REDO_LOG', 'SCHEDULER_PROGRAM_ARGS_TBL', 'SCHEDULER_JOB_ARGS_TBL')");
        else
            return _connection.Query<string>("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'");

    }

    public IEnumerable<(string COLUMN_NAME, string DATA_TYPE)> GetSchemaForTable(string table)
    {
        if (_connection is OracleConnection)
            return _connection.Query<(string COLUMN_NAME, string DATA_TYPE)>(
                    "SELECT COLUMN_NAME, DATA_TYPE FROM ALL_TAB_COLUMNS WHERE TABLE_NAME = :TableName",
                    new { TableName = table.ToUpper() } // Oracle stores object names in uppercase by default
                );
        else
            return _connection.Query<(string COLUMN_NAME, string DATA_TYPE)>(
                    "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName",
                    new { TableName = table }
                );
    }

    private Task<object?> OracleExecuteInsertAsync(string query)
    {
        DbCommand command = _connection.CreateCommand();
        DbParameter? dbParam = command.CreateParameter();
        dbParam.ParameterName = "ret";
        OracleParameter returnId = new OracleParameter("ret", OracleDbType.Int32);
        returnId.Direction = ParameterDirection.Output;
        command.Parameters.Add(returnId);
        command.CommandText = query;
        return Task.Run(async () =>
        {
            await command.ExecuteScalarAsync();
            return returnId.Value;
        });
    }

    private async Task<object?> OracleExecuteInsertAsync(string query, object? parameters)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = query;
        
        // Set up the RETURNING INTO output parameter
        var returnParam = new OracleParameter("ret", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(returnParam);
        
        // Add the input parameters using Dapper's parameter handling
        if (parameters != null)
        {
            var dynamicParams = new DynamicParameters(parameters);
            // Add the output parameter to Dapper's parameter collection
            dynamicParams.Add("ret", dbType: DbType.Int32, direction: ParameterDirection.Output);
            
            // Execute using Dapper to handle both input and output parameters
            await _connection.ExecuteAsync(query, dynamicParams);
            
            // Return the output parameter value
            return dynamicParams.Get<int>("ret");
        }
        else
        {
            // No input parameters, just execute with output parameter
            await command.ExecuteNonQueryAsync();
            return returnParam.Value;
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}