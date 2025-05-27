using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using System.Dynamic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GenericTableAPI.Utilities;

public static class DatabaseUtilities
{
    public enum DatabaseType
    {
        SqlServer,
        Oracle,
        Unknown
    }

    private static readonly Regex TableNameRegex = new(@"^\w+$", RegexOptions.Compiled);

    public static DatabaseType GetDatabaseType(string? connectionString)
    {
        if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

        connectionString = connectionString.ToUpper(CultureInfo.InvariantCulture);

        if (connectionString.Contains("DATA SOURCE=") && connectionString.Contains("USER ID="))
        {
            return DatabaseType.Oracle;
        }

        if ((connectionString.Contains("SERVER = (") || connectionString.Contains("SERVER=(")) && 
            (connectionString.Contains("(HOST=") || connectionString.Contains("(HOST =")))
        {
            return DatabaseType.Oracle;
        }

        if (connectionString.Contains("SERVER=") && connectionString.Contains("DATABASE="))
        {
            return DatabaseType.SqlServer;
        }

        return DatabaseType.Unknown;
    }
    /// <summary>
    /// Validates that a table name contains only safe characters
    /// </summary>
    private static void ValidateTableName(string? tableName)
    {
        if (string.IsNullOrEmpty(tableName) || !TableNameRegex.IsMatch(tableName))
        {
            throw new ArgumentException("Invalid table name. Only alphanumeric characters and underscores are allowed.");
        }
    }

    /// <summary>
    /// Retrieves the primary key column name for a given table using parameterized queries
    /// </summary>
    public static async Task<string?> GetPrimaryKeyColumnNameAsync(string? connectionString, string tableName, DatabaseType databaseType)
    {
        ValidateTableName(tableName);

        string query = databaseType switch
        {
            DatabaseType.SqlServer => @"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1
                AND TABLE_NAME = @tableName",
            DatabaseType.Oracle => @"
                SELECT cols.column_name
                FROM all_constraints cons, all_cons_columns cols
                WHERE cons.constraint_type = 'P'
                AND cons.constraint_name = cols.constraint_name
                AND cons.owner = cols.owner
                AND UPPER(cols.table_name) = :tableName",
            _ => throw new NotSupportedException("Unsupported database type.")
        };

        using DbConnection connection = databaseType switch
        {
            DatabaseType.SqlServer => new SqlConnection(connectionString),
            DatabaseType.Oracle => new OracleConnection(connectionString),
            _ => throw new NotSupportedException("Unsupported database type.")
        };

        await connection.OpenAsync();

        using DbCommand command = connection.CreateCommand();
        command.CommandText = query;

        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = databaseType == DatabaseType.Oracle ? "tableName" : "@tableName";
        parameter.Value = databaseType == DatabaseType.Oracle ? tableName.ToUpper() : tableName;
        command.Parameters.Add(parameter);

        using DbDataReader reader = await command.ExecuteReaderAsync();
        return reader.Read() ? reader["COLUMN_NAME"]?.ToString() : null;
    }
    /// <summary>
    /// Synchronous version of GetPrimaryKeyColumnNameAsync for backward compatibility
    /// </summary>
    public static string? GetPrimaryKeyColumnName(string? connectionString, string tableName, DatabaseType databaseType)
    {
        return GetPrimaryKeyColumnNameAsync(connectionString, tableName, databaseType).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Converts DbDataReader to dynamic objects asynchronously
    /// </summary>
    public static async IAsyncEnumerable<dynamic> ToDynamicListAsync(DbDataReader reader)
    {
        while (await reader.ReadAsync())
        {
            dynamic expandoObject = new ExpandoObject();
            IDictionary<string, object> dictionary = expandoObject;

            for (int i = 0; i < reader.FieldCount; i++)
            {
                dictionary[reader.GetName(i)] = reader.GetValue(i);
            }

            yield return expandoObject;
        }
    }

    /// <summary>
    /// Legacy method - kept for backward compatibility but marked as obsolete
    /// </summary>
    [Obsolete("Use ToDynamicListAsync instead for better naming consistency")]
    public static async IAsyncEnumerable<dynamic> ToDynamicList(DbDataReader reader)
    {
        await foreach (var item in ToDynamicListAsync(reader))
        {
            yield return item;
        }
    }
    /// <summary>
    /// Converts the first row of DbDataReader to a dynamic object
    /// </summary>
    public static dynamic? ToDynamic(DbDataReader reader)
    {
        if (!reader.Read()) return null;
        
        dynamic expandoObject = new ExpandoObject();
        IDictionary<string, object> dictionary = expandoObject;

        for (int i = 0; i < reader.FieldCount; i++)
        {
            dictionary[reader.GetName(i)] = reader.GetValue(i);
        }

        return expandoObject;
    }

    /// <summary>
    /// Safely executes a parameterized query and returns the results as dynamic objects
    /// </summary>
    public static async Task<IEnumerable<dynamic>> ExecuteQueryAsync(
        string connectionString, 
        string query, 
        Dictionary<string, object?>? parameters = null)
    {
        DatabaseType dbType = GetDatabaseType(connectionString);
        
        using DbConnection connection = dbType switch
        {
            DatabaseType.SqlServer => new SqlConnection(connectionString),
            DatabaseType.Oracle => new OracleConnection(connectionString),
            _ => throw new NotSupportedException("Unknown database type.")
        };

        await connection.OpenAsync();

        using DbCommand command = connection.CreateCommand();
        command.CommandText = query;

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                DbParameter dbParam = command.CreateParameter();
                dbParam.ParameterName = dbType == DatabaseType.Oracle ? param.Key : $"@{param.Key}";
                dbParam.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(dbParam);
            }
        }

        using DbDataReader reader = await command.ExecuteReaderAsync();
        var results = new List<dynamic>();
        
        await foreach (var item in ToDynamicListAsync(reader))
        {
            results.Add(item);
        }

        return results;
    }
    /// <summary>
    /// Safely executes a parameterized scalar query
    /// </summary>
    public static async Task<object?> ExecuteScalarAsync(
        string connectionString, 
        string query, 
        Dictionary<string, object?>? parameters = null)
    {
        DatabaseType dbType = GetDatabaseType(connectionString);
        
        using DbConnection connection = dbType switch
        {
            DatabaseType.SqlServer => new SqlConnection(connectionString),
            DatabaseType.Oracle => new OracleConnection(connectionString),
            _ => throw new NotSupportedException("Unknown database type.")
        };

        await connection.OpenAsync();

        using DbCommand command = connection.CreateCommand();
        command.CommandText = query;

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                DbParameter dbParam = command.CreateParameter();
                dbParam.ParameterName = dbType == DatabaseType.Oracle ? param.Key : $"@{param.Key}";
                dbParam.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(dbParam);
            }
        }

        return await command.ExecuteScalarAsync();
    }
    /// <summary>
    /// Safely executes a parameterized non-query command (INSERT, UPDATE, DELETE)
    /// </summary>
    public static async Task<int> ExecuteNonQueryAsync(
        string connectionString, 
        string query, 
        Dictionary<string, object?>? parameters = null)
    {
        DatabaseType dbType = GetDatabaseType(connectionString);
        
        using DbConnection connection = dbType switch
        {
            DatabaseType.SqlServer => new SqlConnection(connectionString),
            DatabaseType.Oracle => new OracleConnection(connectionString),
            _ => throw new NotSupportedException("Unknown database type.")
        };

        await connection.OpenAsync();

        using DbCommand command = connection.CreateCommand();
        command.CommandText = query;

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                DbParameter dbParam = command.CreateParameter();
                dbParam.ParameterName = dbType == DatabaseType.Oracle ? param.Key : $"@{param.Key}";
                dbParam.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(dbParam);
            }
        }

        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Validates and sanitizes a list of column names for ORDER BY clauses
    /// </summary>
    public static string ValidateColumnNames(string? columnNames)
    {
        if (string.IsNullOrEmpty(columnNames))
            return string.Empty;

        var columns = columnNames.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var validatedColumns = new List<string>();

        foreach (var column in columns)
        {
            var trimmedColumn = column.Trim();
            var parts = trimmedColumn.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 0 || parts.Length > 2)
                throw new ArgumentException($"Invalid column specification: {trimmedColumn}");

            if (!TableNameRegex.IsMatch(parts[0]))
                throw new ArgumentException($"Invalid column name: {parts[0]}");

            if (parts.Length == 2)
            {
                var direction = parts[1].ToUpperInvariant();
                if (direction != "ASC" && direction != "DESC")
                    throw new ArgumentException($"Invalid sort direction: {parts[1]}");
                
                validatedColumns.Add($"{parts[0]} {direction}");
            }
            else
            {
                validatedColumns.Add(parts[0]);
            }
        }

        return string.Join(", ", validatedColumns);
    }

    /// <summary>
    /// Creates secure SET clauses for UPDATE queries with proper value escaping
    /// </summary>
    public static string CreateSecureSetClauses(IDictionary<string, object?> values)
    {
        var setClauses = new List<string>();
        
        foreach (var kvp in values)
        {
            // Validate column name
            if (!System.Text.RegularExpressions.Regex.IsMatch(kvp.Key, @"^\w+$"))
                throw new ArgumentException($"Invalid column name: {kvp.Key}");
            
            string safeValue;
            if (kvp.Value == null)
            {
                safeValue = "NULL";
            }
            else if (kvp.Value is string stringValue)
            {
                // Escape single quotes in string values
                safeValue = $"'{stringValue.Replace("'", "''")}'";
            }
            else if (kvp.Value is int || kvp.Value is long || kvp.Value is decimal || kvp.Value is float || kvp.Value is double)
            {
                safeValue = kvp.Value.ToString();
            }
            else if (kvp.Value is bool boolValue)
            {
                safeValue = boolValue ? "1" : "0";
            }
            else if (kvp.Value is DateTime dateTimeValue)
            {
                safeValue = $"'{dateTimeValue:yyyy-MM-dd HH:mm:ss}'";
            }
            else
            {
                // For other types, convert to string and escape
                safeValue = $"'{kvp.Value.ToString()?.Replace("'", "''")}'" ?? "NULL";
            }
            
            setClauses.Add($"{kvp.Key} = {safeValue}");
        }
        
        return string.Join(", ", setClauses);
    }

    /// <summary>
    /// Validates and sanitizes a single value for safe SQL usage
    /// </summary>
    public static object? SanitizeValue(object? value)
    {
        if (value == null) return null;
        
        if (value is string stringValue)
        {
            // Remove or escape potentially dangerous characters
            return stringValue.Replace("'", "''")
                             .Replace(";", "")
                             .Replace("--", "")
                             .Replace("/*", "")
                             .Replace("*/", "")
                             .Replace("xp_", "")
                             .Replace("sp_", "");
        }
        
        return value;
    }
}