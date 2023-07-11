using System.Data.Common;
using System.Dynamic;
using System.Text;
using System.Text.RegularExpressions;
using GenericTableAPI.Services;
using GenericTableAPI.Utilities;
using Microsoft.IdentityModel.Tokens;
using static GenericTableAPI.Utilities.DatabaseUtilities;

namespace GenericTableAPI.Repositories;

public class DapperRepository
{
    private readonly string? _connectionString;
    private readonly string? _schemaName;
    private readonly Serilog.ILogger _logger;

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

    public DapperRepository(string? connectionString, string? schemaName, Serilog.ILogger logger)
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
    public async Task<IEnumerable<dynamic>?> GetAllAsync(string tableName, string? where = null, string? orderBy = null, int? limit = null)
    {
        DatabaseSyntaxService syntaxService = new DatabaseSyntaxService();

        using DatabaseHandler connectionHandler = new(_connectionString);
        connectionHandler.Open();

        string query = syntaxService.GetAllQuery(tableName, where, orderBy, limit, _connectionString);
        try
        {
            List<dynamic>? result = new();
            await foreach (dynamic item in ToDynamicList(await connectionHandler.ExecuteReaderAsync(query)))
            {
                result.Add(item);
            }
            return result;
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "An error occurred while executing GetAllAsync for query: {0}", query);
            return null;
        }
        finally
        {
            connectionHandler.Close();

        }
    }

    /// <summary>
    /// Returns a single row from a table by its primary key
    /// </summary>
    /// <param name="tableName">Table name</param>
    /// <param name="primaryKey">Primary key</param>
    /// <returns></returns>
    public async Task<dynamic?> GetByIdAsync(string tableName, string primaryKey, string primaryKeyColumnName = "")
    {
        DatabaseSyntaxService syntaxService = new DatabaseSyntaxService();

        if(string.IsNullOrEmpty(primaryKeyColumnName))
            primaryKeyColumnName = GetPrimaryKeyColumnName(_connectionString, tableName, GetDatabaseType(_connectionString));
        _logger.Information("Primary key column name: {0} used for table: {1} in GetByIdAsync", primaryKeyColumnName, tableName);
        
        using DatabaseHandler connectionHandler = new(_connectionString);
        connectionHandler.Open();
        string query = syntaxService.GetByIdQuery(tableName, primaryKey, primaryKeyColumnName);

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
            _logger.Error(exception, "An error occurred while executing GetByIdAsync for query: {0}", query);
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
    public async Task<object?> AddAsync(string tableName, IDictionary<string, object?> values, string primaryKeyColumnName = "")
    {
        // Validate and sanitize each column value
        foreach ((string? columnName, object? columnValue) in values)
        {
            DatabaseSyntaxService syntaxService = new DatabaseSyntaxService();

            if (!Regex.IsMatch(columnName, @"^[\w\d]+$|^[\w\d]+$"))
            {
                throw new ArgumentException("Invalid column name");
            }

            // Sanitize the column value to prevent SQL injection
            object? sanitizedValue = SanitizeValue(columnValue);
            if (string.IsNullOrEmpty(primaryKeyColumnName))
                primaryKeyColumnName = GetPrimaryKeyColumnName(_connectionString, tableName, GetDatabaseType(_connectionString));
            _logger.Information("Primary key column name: {0} used for table: {1} in AddAsync", primaryKeyColumnName, tableName);
            string columns = string.Join(", ", values.Keys);
            string results = string.Join(", ", values.Values.Select(k => $"'{k}'"));

            string sql = syntaxService.AddQuery(tableName, values, columns, results, primaryKeyColumnName, _connectionString);

            using DatabaseHandler connectionHandler = new(_connectionString);

            connectionHandler.Open();

            try
            {
                object? result = await connectionHandler.ExecuteScalarAsync(sql);
                return result;
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "An error occurred while executing AddAsync for query: {0}", sql);
                throw;
            }
            finally
            {
                connectionHandler.Close();
            }
        }
        return null;
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
    public async Task<bool> UpdateAsync(string tableName, string primaryKey, IDictionary<string, object?> values, string primaryKeyColumnName = "")
    {
        DatabaseSyntaxService syntaxService = new DatabaseSyntaxService();

        Dictionary<string, object>? sanitizedValues = new();
        if (sanitizedValues == null)
        {
            throw new ArgumentNullException(nameof(sanitizedValues));}
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

        if (string.IsNullOrEmpty(primaryKeyColumnName))
            primaryKeyColumnName = GetPrimaryKeyColumnName(_connectionString, tableName, GetDatabaseType(_connectionString));
        _logger.Information("Primary key column name: {0} used for table: {1} in UpdateAsync", primaryKeyColumnName, tableName);

        using DatabaseHandler connectionHandler = new(_connectionString);
        connectionHandler.Open();
        string query = syntaxService.UpdateQuery(tableName, primaryKey, values, primaryKeyColumnName, setClauses);

        try
        {
            await connectionHandler.ExecuteScalarAsync(query);
            return true;
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "An error occurred while executing UpdateAsync for query: {0}", query);
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
    public async Task<bool> DeleteAsync(string tableName, string primaryKey, string primaryKeyColumnName = "")
    {
        DatabaseSyntaxService syntaxService = new DatabaseSyntaxService();

        if (string.IsNullOrEmpty(primaryKeyColumnName))
            primaryKeyColumnName = GetPrimaryKeyColumnName(_connectionString, tableName, GetDatabaseType(_connectionString));

        _logger.Information("Primary key column name: {0} used for table: {1} in DeleteAsync", primaryKeyColumnName, tableName);
        using DatabaseHandler connectionHandler = new(_connectionString);
        connectionHandler.Open();
        string query = syntaxService.DeleteQuery(tableName, primaryKey, primaryKeyColumnName);

        try
        {
            await connectionHandler.ExecuteScalarAsync(query);
            return true;
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "An error occurred while executing DeleteAsync for query: {0}", query);
            return false;
        }
        finally
        {
            connectionHandler.Close();
        }
    }
}
