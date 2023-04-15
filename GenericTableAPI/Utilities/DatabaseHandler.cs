using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using static GenericTableAPI.Utilities.DatabaseUtilities;
using System.Data.Common;

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
        Task<object?> result = command.ExecuteScalarAsync();
        return result;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}