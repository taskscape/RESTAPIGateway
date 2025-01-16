using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using static GenericTableAPI.Utilities.DatabaseUtilities;
using System.Data.Common;
using System.Data;

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

    public Task<object?> ExecuteScalarAsync(string query)
    {
        DbCommand command = _connection.CreateCommand();
        command.CommandText = query;
        return command.ExecuteScalarAsync();
    }

    public Task<object?> ExecuteInsertAsync(string query)
    {
        if (_connection is OracleConnection)
            return OracleExecuteInsertAsync(query);
        else
            return ExecuteScalarAsync(query);
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

    public void Dispose()
    {
        _connection.Dispose();
    }
}