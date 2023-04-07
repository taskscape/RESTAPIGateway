using System.Data.Common;
using System.Dynamic;
using System.Text.RegularExpressions;
using GenericTableAPI.Utilities;
using static GenericTableAPI.Utilities.DatabaseUtilities;

namespace GenericTableAPI.Repositories
{
    public class DapperRepository
    {
        private readonly string? _connectionString;

        private static object? SanitizeValue(object? value)
        {
            // Sanitize the value by replacing any single quotes with two single quotes
            string? sanitizedValue = value.ToString()?.Replace("'", "''");

            return sanitizedValue;
        }

        public DapperRepository(string? connectionString)
        {
            _connectionString = connectionString;
        }

        public static DatabaseType DetectDatabaseType(string connectionString)
        {
            if (connectionString.Contains("Data Source=") && connectionString.Contains("User Id="))
            {
                return DatabaseType.Oracle;
            }

            if (connectionString.Contains("Server=") && connectionString.Contains("Database="))
            {
                return DatabaseType.SqlServer;
            }

            return DatabaseType.Unknown;
        }

        public async Task<IEnumerable<dynamic>> GetAllAsync(string tableName)
        {
            using DatabaseHandler connectionHandler = new(_connectionString);
            connectionHandler.Open();
            string sql = $"SELECT * FROM {tableName}";
            var result = new List<dynamic>();
            await foreach (dynamic item in ToDynamicList(await connectionHandler.ExecuteReaderAsync(sql)))
            {
                result.Add(item);
            }
            connectionHandler.Close();
            return result;
        }

        public async Task<dynamic?> GetByIdAsync(string tableName, string id)
        {
            string primaryKeyColumn = GetPrimaryKeyColumnName(_connectionString, tableName, GetDatabaseType(_connectionString));
            using DatabaseHandler connectionHandler = new(_connectionString);
            connectionHandler.Open();
            await using DbDataReader reader = await connectionHandler.ExecuteReaderAsync($"SELECT * FROM {tableName} WHERE {primaryKeyColumn} = {id};");
            if (await reader.ReadAsync())
            {
                dynamic? result = new ExpandoObject();
                var dict = (IDictionary<string, object>)result;
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dict.Add(reader.GetName(i), reader.GetValue(i));
                }
                connectionHandler.Close();
                return result;
            }

            connectionHandler.Close();
            return null;
        }

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
            string primaryKeyColumn = GetPrimaryKeyColumnName(_connectionString, tableName, GetDatabaseType(_connectionString));

            using DatabaseHandler connectionHandler = new(_connectionString);
            connectionHandler.Open();
            object? id = await connectionHandler.ExecuteScalarAsync($"INSERT INTO {tableName} ({columns}) OUTPUT Inserted.{primaryKeyColumn} VALUES ({strValues});");
            connectionHandler.Close();

            return id;
        }

        public async Task UpdateAsync(string tableName, string id, IDictionary<string, object?> values)
        {
            var sanitizedValues = new Dictionary<string, object>();
            if (sanitizedValues == null) throw new ArgumentNullException(nameof(sanitizedValues));
            foreach (var kvp in values)
            {
                if (!Regex.IsMatch(kvp.Key, @"^[\w\d]+$"))
                {
                    throw new ArgumentException("Invalid column name");
                }

                object? sanitizedValue = SanitizeValue(kvp.Value);
                sanitizedValues.Add(kvp.Key, sanitizedValue);
            }

            string setClauses = string.Join(", ", values.Select(k => $"{k.Key} = '{k.Value}'"));

            string primaryKeyColumn = GetPrimaryKeyColumnName(_connectionString, tableName, GetDatabaseType(_connectionString));
            using DatabaseHandler connectionHandler = new(_connectionString);
            connectionHandler.Open();
            await connectionHandler.ExecuteScalarAsync($"UPDATE {tableName} SET {setClauses} WHERE {primaryKeyColumn} = {id};");
            connectionHandler.Close();
        }

        public async Task DeleteAsync(string tableName, string id)
        {
            string primaryKeyColumn = GetPrimaryKeyColumnName(_connectionString, tableName, GetDatabaseType(_connectionString));
            using DatabaseHandler connectionHandler = new(_connectionString);
            connectionHandler.Open();
            await connectionHandler.ExecuteScalarAsync($"DELETE FROM {tableName} WHERE {primaryKeyColumn} = {id};");
            connectionHandler.Close();
        }
    }
}
