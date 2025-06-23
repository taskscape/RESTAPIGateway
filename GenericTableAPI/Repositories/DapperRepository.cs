using System.Data.Common;
using System.Dynamic;
using System.Text.RegularExpressions;
using GenericTableAPI.Models;
using GenericTableAPI.Services;
using GenericTableAPI.Utilities;
using Microsoft.Data.SqlClient;
using static GenericTableAPI.Utilities.DatabaseUtilities;

namespace GenericTableAPI.Repositories;

public partial class DapperRepository(string? connectionString, string? schemaName, Serilog.ILogger logger)
{
    private static object? SanitizeValue(object? value)
    {
        if (value == null) return null;

        string raw = value.ToString() ?? "";

        // Regex to detect SQL injection attempts
        string pattern = @"('.*--)|(;)|(/\*)|(\*/)|('{2,})|(\b(SELECT|INSERT|DELETE|UPDATE|DROP|EXEC|UNION|OR|AND)\b)";
        if (Regex.IsMatch(raw, pattern, RegexOptions.IgnoreCase))
        {
            throw new ArgumentException($"SanitizeValue: Possible SQL injection attempt detected in value: {raw}");
        }

        // Escape single quotes
        string sanitizedValue = raw.Replace("'", "''");
        return $"'{sanitizedValue}'";
    }

    /// <summary>
    /// Returns all rows from a given table
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="where"></param>
    /// <param name="orderBy"></param>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns>List of objects</returns>
    public async Task<IEnumerable<dynamic>?> GetAllAsync(string tableName, string? where = null, string? orderBy = null, int? limit = null, int? offset = null)
    {
        using DatabaseHandler connectionHandler = new(connectionString);
        connectionHandler.Open();

        string query = SyntaxService.GetAllQuery(tableName, schemaName, where, orderBy, limit, offset, connectionString);
        try
        {
            logger.Information("Repository.GetAllAsync: executing: " + query);
            IAsyncEnumerable<dynamic> results = ToDynamicList(await connectionHandler.ExecuteReaderAsync(query));

            List<dynamic>? result = [];
            await foreach (dynamic item in results)
            {
                result.Add(item);
            }
            return result;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Repository.GetAllAsync: An error occurred while executing: {0}", query);
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
    /// <param name="columnName">Optional column name to use for primaryKey, if not default</param>
    /// <returns></returns>
    public async Task<dynamic?> GetByIdAsync(string tableName, string primaryKey, string? columnName = "")
    {
        if (string.IsNullOrEmpty(columnName))
        {
            columnName = GetPrimaryKeyColumnName(connectionString, tableName, GetDatabaseType(connectionString));
        }
        
        logger.Information("Repository.GetByIdAsync: Primary key column name: {0} used for table: {1}", columnName, tableName);

        using DatabaseHandler connectionHandler = new(connectionString);
        connectionHandler.Open();
        string query = SyntaxService.GetByIdQuery(tableName, schemaName, primaryKey, columnName);

        try
        {
            logger.Information("Repository.GetByIdAsync: executing: " + query);
            await using DbDataReader reader = await connectionHandler.ExecuteReaderAsync(query);
            if (await reader.ReadAsync())
            {
                dynamic? result = new ExpandoObject();
                IDictionary<string, object> dictionary = (IDictionary<string, object>)result;
                for (int columnIndex = 0; columnIndex < reader.FieldCount; columnIndex++)
                {
                    dictionary.Add(reader.GetName(columnIndex), reader.GetValue(columnIndex));
                }

                connectionHandler.Close();
                return result;
            }
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Repository.GetByIdAsync: An error occurred while executing: {0}", query);
        }

        connectionHandler.Close();
        return null;
    }

    /// <summary>
    /// Creates new row in a given table
    /// </summary>
    /// <param name="tableName">Table name</param>
    /// <param name="values">values to insert</param>
    /// <param name="columnName">Optional column name to use for primaryKey, if not default</param>
    /// <returns>Identifier of a newly created row</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<object?> AddAsync(string tableName, IDictionary<string, object?> values, string? columnName = "")
    {
        // Validate and sanitize each column value
        foreach ((string? key, object? value) in values)
        {
            if (!Regex.IsMatch(key, @"^[\w\d]+$|^[\w\d]+$"))
            {
                throw new ArgumentException("Repository.AddAsync: Invalid column name: " + key);
            }

            // Sanitize the column value to prevent SQL injection
            object? sanitizedValue = SanitizeValue(value);

            // Add the sanitized value to the SQL insert statement
            values[key] = sanitizedValue;
        }

        if (string.IsNullOrEmpty(columnName))
        {
            columnName = GetPrimaryKeyColumnName(connectionString, tableName, GetDatabaseType(connectionString));
        }

        logger.Information("Repository.AddAsync: Primary key column name: {0} used for table: {1}", columnName, tableName);

        string columns = string.Join(", ", values.Keys);
        string results = string.Join(", ", values.Values.Select(k => $"'{k}'"));

        string query = SyntaxService.AddQuery(tableName, schemaName, values, columns, results, columnName, connectionString);

        using DatabaseHandler connectionHandler = new(connectionString);
        connectionHandler.Open();

        try
        {
            logger.Information("Repository.AddAsync: executing: " + query);
            object? result = await connectionHandler.ExecuteInsertAsync(query);
            return result;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Repository.AddAsync: An error occurred while executing: {0}", query);
            throw;
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
    /// <param name="columns"></param>
    /// <param name="columnName">Optional column name to use for primaryKey, if not default</param>
    /// <returns>True on success</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> UpdateAsync(string tableName, string primaryKey, IDictionary<string, object?> values,List<object> columns, string? columnName = "")
    {
        Dictionary<string, object>? sanitizedValues = new();
        if (sanitizedValues == null)
        {
            logger.Error("Repository.UpdateAsync: sanitizedValues is null");
            throw new ArgumentNullException(nameof(sanitizedValues));
        }

        foreach (KeyValuePair<string, object?> pair in values)
        {
            if (!Regex.IsMatch(pair.Key, @"^[A-Za-z_][A-Za-z0-9_]*$"))
            {
                throw new ArgumentException("Repository.UpdateAsync: Invalid column name: " + columnName);
            }

            object? sanitizedValue = SanitizeValue(pair.Value);
            if (sanitizedValue != null) sanitizedValues.Add(pair.Key, sanitizedValue);
        }

        if (string.IsNullOrEmpty(columnName))
        {
            columnName = GetPrimaryKeyColumnName(connectionString, tableName, GetDatabaseType(connectionString));
        }
        
        foreach (object column in columns)
        {
            if (values.Keys.All(k => !string.Equals(k, column.ToString(), StringComparison.CurrentCultureIgnoreCase)) && !string.Equals((string)column, columnName, StringComparison.OrdinalIgnoreCase))
            { 
                values.Add(column.ToString(), null);   
            }
        }
        string setClauses = string.Join(", ", values.Select(k => $"{k.Key} = '{k.Value}'"));

        logger.Information("Repository.UpdateAsync: Primary key column name: {0} used for table: {1}", columnName, tableName);

        using DatabaseHandler connectionHandler = new(connectionString);
        connectionHandler.Open();
        if (connectionString != null)
        {
            string query = SyntaxService.MergeQuery(tableName, schemaName, primaryKey, values, connectionString, columnName, setClauses);

            try
            {
                logger.Information("Repository.UpdateAsync: executing: " + query);
                await connectionHandler.ExecuteScalarAsync(query);
                return true;
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Repository.UpdateAsync: An error occurred while executing: {0}", query);
                throw;
            }
            finally
            {
                connectionHandler.Close();
            }
        }

        return false;
    }
    
    /// <summary>
    /// Patches a row in a given table
    /// </summary>
    /// <param name="tableName">Table name</param>
    /// <param name="primaryKey">Primary key</param>
    /// <param name="values">values to update</param>
    /// <param name="columnName">Optional column name to use for primaryKey, if not default</param>
    /// <returns>True on success</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> PatchAsync(string tableName, string primaryKey, IDictionary<string, object?> values, string? columnName = "")
    {
        Dictionary<string, object>? sanitizedValues = new();
        if (sanitizedValues == null)
        {
            logger.Error("Repository.PatchAsync: sanitizedValues is null");
            throw new ArgumentNullException(nameof(sanitizedValues));
        }

        foreach (KeyValuePair<string, object?> pair in values)
        {
            if (!MyRegex().IsMatch(pair.Key))
            {
                throw new ArgumentException("Repository.PatchAsync: Invalid column name: " + columnName);
            }

            object? sanitizedValue = SanitizeValue(pair.Value);
            if (sanitizedValue != null) sanitizedValues.Add(pair.Key, sanitizedValue);
        }

        string setClauses = string.Join(", ", values.Select(k => $"{k.Key} = '{k.Value}'"));

        if (string.IsNullOrEmpty(columnName))
        {
            columnName = GetPrimaryKeyColumnName(connectionString, tableName, GetDatabaseType(connectionString));
        }

        logger.Information("Repository.PatchAsync: Primary key column name: {0} used for table: {1}", columnName, tableName);

        using DatabaseHandler connectionHandler = new(connectionString);
        connectionHandler.Open();
        string query = SyntaxService.UpdateQuery(tableName, schemaName, primaryKey, values, columnName, setClauses);

        try
        {
            logger.Information("Repository.PatchAsync: executing: " + query);
            await connectionHandler.ExecuteScalarAsync(query);
            return true;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Repository.PatchAsync: An error occurred while executing: {0}", query);
            throw;
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
    /// <param name="columnName">Optional column name to use for primaryKey, if not default</param>
    /// <returns>True on success</returns>
    public async Task<bool> DeleteAsync(string tableName, string primaryKey, string? columnName = "")
    {
        if (string.IsNullOrEmpty(columnName))
        {
            columnName = GetPrimaryKeyColumnName(connectionString, tableName, GetDatabaseType(connectionString));
        }

        logger.Information("Repository.DeleteAsync: Primary key column name: {0} used for table: {1}", columnName, tableName);

        using DatabaseHandler connectionHandler = new(connectionString);
        connectionHandler.Open();
        string query = SyntaxService.DeleteQuery(tableName, schemaName, primaryKey, columnName);

        try
        {
            logger.Information("Repository.DeleteAsync: executing: " + query);
            await connectionHandler.ExecuteScalarAsync(query);
            return true;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Repository.DeleteAsync: An error occurred while executing: {0}", query);
            throw;
        }
        finally
        {
            connectionHandler.Close();
        }
    }
    
    /// <summary>
    /// Executes a stored procedure asynchronously and returns the results as a list of objects.
    /// </summary>
    /// <param name="procedureName">The name of the stored procedure to execute.</param>
    /// <param name="values">A collection of parameters to pass to the stored procedure.</param>
    /// <returns>A list of objects representing the rows returned by the stored procedure, or null if an error occurs.</returns>
    /// <exception cref="Exception">Thrown if an error occurs while executing the stored procedure.</exception>
    public async Task<List<object>?> ExecuteAsync(string procedureName, IEnumerable<StoredProcedureParameter?>? values)
    {
        using DatabaseHandler connectionHandler = new(connectionString);
        connectionHandler.Open();
        
        string query = SyntaxService.ExecuteQuery(procedureName, values, connectionString);

        try
        {
            logger.Information("Repository.ExecuteAsync: executing: " + query);
            IAsyncEnumerable<dynamic> results = ToDynamicList(await connectionHandler.ExecuteReaderAsync(query));
            List<dynamic> result = new();
            await foreach (dynamic item in results)
            {
                result.Add(item);
            }
            return result;
        }
        catch (SqlException exception)
        {
            logger.Error(exception, "Repository.ExecuteAsync: An error occurred while executing: {0}", query);
            throw new Exception();
        }
        finally
        {
            connectionHandler.Close();
        }
    }
    
    /// <summary>
    /// Returns all column names from a given table
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns>List of objects</returns>
    public async Task<List<object>?> GetColumnsAsync(string tableName)
    {
        using DatabaseHandler connectionHandler = new(connectionString);
        connectionHandler.Open();

        string query = SyntaxService.GetColumnsQuery(tableName, schemaName, connectionString);
        try
        {
            logger.Information("Repository.GetColumnsAsync: executing: " + query);
            IAsyncEnumerable<dynamic> results = ToDynamicList(await connectionHandler.ExecuteReaderAsync(query));

            List<dynamic>? result = new();
            await foreach (dynamic item in results)
            {
                IDictionary<string, object> propertyValues = item;

                foreach (string? property in propertyValues.Keys)
                {
                    result.Add(propertyValues[property]);
                } 
            }
            return result;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Repository.GetColumnsAsync: An error occurred while executing: {0}", query);
            return null;
        }
        finally
        {
            connectionHandler.Close();
        }
    }

    [GeneratedRegex(@"^[\w\d]+$")]
    private static partial Regex MyRegex();
}
