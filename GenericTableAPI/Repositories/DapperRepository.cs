using System.Data.Common;
using System.Dynamic;
using System.Text.RegularExpressions;
using GenericTableAPI.Utilities;
using static GenericTableAPI.Utilities.DatabaseUtilities;

namespace GenericTableAPI.Repositories;

public class DapperRepository
{
    private readonly string? _connectionString;
    private readonly string? _schemaName;
    private readonly ILogger _logger;

    private static object? SanitizeValue(object? value)
    {
        string? sanitizedValue = value?.ToString()?.Replace("'", "''");

        return sanitizedValue;
    }

    /// <summary>
    /// Returns the table name with schemaName if it is not null
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="schemaName"></param>
    /// <returns><see cref="string"/></returns>
    private static string GetTableName(string tableName, string? schemaName)
    {
        return string.IsNullOrEmpty(schemaName) ? tableName : $"{schemaName}.{tableName}";
    }

    public DapperRepository(string? connectionString, string? schemaName, ILogger logger)
    {
        _connectionString = connectionString;
        _schemaName = schemaName;
        _logger = logger;
    }

    /// <summary>
    /// Returns all rows from a given table
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns>List of objects</returns>
    public async Task<IEnumerable<dynamic>> GetAllAsync(string tableName)
    {
        using DatabaseHandler connectionHandler = new(_connectionString);
        connectionHandler.Open();

        string query = $"SELECT * FROM {GetTableName(tableName, _schemaName)}";
        try
        {
            List<dynamic> result = new();
            await foreach (dynamic item in ToDynamicList(await connectionHandler.ExecuteReaderAsync(query)))
            {
                result.Add(item);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occurred while executing GetAllAsync for query: " + query);
        }

        connectionHandler.Close();
        return result;
    }

    /// <summary>
    /// Returns a single row from a table by its primary key
    /// </summary>
    /// <param name="tableName">Table name</param>
    /// <param name="primaryKey">Primary key</param>
    /// <returns></returns>
    public async Task<dynamic?> GetByIdAsync(string tableName, string primaryKey)
    {
        string primaryKeyColumn = GetPrimaryKeyColumnName(_connectionString, GetTableName(tableName, _schemaName), GetDatabaseType(_connectionString));
        using DatabaseHandler connectionHandler = new(_connectionString);
        connectionHandler.Open();
        string query = $"SELECT * FROM {GetTableName(tableName, _schemaName)} WHERE {primaryKeyColumn} = {primaryKey};";

        try
        {
            await using DbDataReader reader = await connectionHandler.ExecuteReaderAsync(query);
            if (await reader.ReadAsync())
            {
                dynamic? result = new ExpandoObject();
                IDictionary<string, object> dictionary = (IDictionary<string, object>)result;
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dictionary.Add(reader.GetName(i), reader.GetValue(i));
                }

                connectionHandler.Close();
                return result;
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occurred while executing GetByIdAsync for query: " + query);
        }

        connectionHandler.Close();
        return null;
    }

    /// <summary>
    /// Creates new row in a given table
    /// </summary>
    /// <param name="tableName">Table name</param>
    /// <param name="values">values to insert</param>
    /// <returns>Identifier of a newly created row</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<object?> AddAsync(string tableName, IDictionary<string, object?> values)
    {
        // Validate and sanitize each column value
        foreach ((string? columnName, object? columnValue) in values)
        {
            if (!Regex.IsMatch(columnName, @"^[\w\d]+$|^[\w\d]+$"))
            {
                throw new ArgumentException("Invalid column name");
            }

            // Sanitize the column value to prevent SQL injection
            object? sanitizedValue = SanitizeValue(columnValue);

            // Add the sanitized value to the SQL insert statement
            values[columnName] = sanitizedValue;
        }

        string columns = string.Join(", ", values.Keys);
        string strValues = string.Join(", ", values.Values.Select(k => $"'{k}'"));
        string primaryKeyColumn = GetPrimaryKeyColumnName(_connectionString, GetTableName(tableName, _schemaName), GetDatabaseType(_connectionString));

        using DatabaseHandler connectionHandler = new(_connectionString);
        connectionHandler.Open();

        string query =
            $"INSERT INTO {GetTableName(tableName, _schemaName)} ({columns}) OUTPUT Inserted.{primaryKeyColumn} VALUES ({strValues});";

        try
        {
            object? id = await connectionHandler.ExecuteScalarAsync(query);
            return id;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occurred while executing AddAsync for query: " + query);
            return null;
        }
        finally
        {
            connectionHandler.Close();
        }
    }

    /// <summary>
    /// Updates a row in a given table
    /// </summary>
    /// <param name="tableName">Table name</param>
    /// <param name="primaryKey">Primary key</param>
    /// <param name="values">values to update</param>
    /// <returns>True on success</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> UpdateAsync(string tableName, string primaryKey, IDictionary<string, object?> values)
    {
        Dictionary<string, object>? sanitizedValues = new();
        if (sanitizedValues == null) throw new ArgumentNullException(nameof(sanitizedValues));
        foreach (KeyValuePair<string, object?> pair in values)
        {
            if (!Regex.IsMatch(pair.Key, @"^[\w\d]+$"))
            {
                throw new ArgumentException("Invalid column name");
            }

            object? sanitizedValue = SanitizeValue(pair.Value);
            sanitizedValues.Add(pair.Key, sanitizedValue);
        }

        string setClauses = string.Join(", ", values.Select(k => $"{k.Key} = '{k.Value}'"));

        string primaryKeyColumn = GetPrimaryKeyColumnName(_connectionString, GetTableName(tableName, _schemaName), GetDatabaseType(_connectionString));
        using DatabaseHandler connectionHandler = new(_connectionString);
        connectionHandler.Open();
        string query =
            $"UPDATE {GetTableName(tableName, _schemaName)} SET {setClauses} WHERE {primaryKeyColumn} = {primaryKey};";

        try
        {
            await connectionHandler.ExecuteScalarAsync(query);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occurred while executing UpdateAsync for query: " + query);
            return false;
        }
        finally
        {
            connectionHandler.Close();
        }
    }

    /// <summary>
    /// Deletes a row from a table
    /// </summary>
    /// <param name="tableName">Table name</param>
    /// <param name="primaryKey">Primary key</param>
    /// <returns>True on success</returns>
    public async Task<bool> DeleteAsync(string tableName, string primaryKey)
    {
        string primaryKeyColumn = GetPrimaryKeyColumnName(_connectionString, GetTableName(tableName, _schemaName), GetDatabaseType(_connectionString));
        using DatabaseHandler connectionHandler = new(_connectionString);
        connectionHandler.Open();
        string query = $"DELETE FROM {GetTableName(tableName, _schemaName)} WHERE {primaryKeyColumn} = {primaryKey};";

        try
        {
            await connectionHandler.ExecuteScalarAsync(query);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occurred while executing DeleteAsync for query: " + query);
            return false;
        }
        finally
        {
            connectionHandler.Close();

        }
    }
}