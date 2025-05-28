using GenericTableAPI.Helpers;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using System.Dynamic;
using System.Globalization;

namespace GenericTableAPI.Utilities;

public static class DatabaseUtilities
{
    public enum DatabaseType
    {
        SqlServer,
        Oracle,
        Unknown
    }

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
    /// Retrieves the primary key column name for a given table
    /// </summary>
    /// <param name="connectionString">Connection string</param>
    /// <param name="tableName">Table name must be provided without schema</param>
    /// <param name="databaseType">Database type</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static string GetPrimaryKeyColumnName(string? connectionString, string tableName, DatabaseType databaseType)
    {
        QueryHelper.ValidateIdentifier(tableName);

        string query = databaseType switch
        {
            DatabaseType.SqlServer => QueryHelper.Build(
            "SELECT COLUMN_NAME\n",
            "FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE\n",
            "WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1\n",
            "  AND TABLE_NAME = '", tableName, "'"
        ),
            DatabaseType.Oracle => QueryHelper.Build(
                "SELECT cols.column_name\n",
                "FROM all_constraints cons, all_cons_columns cols\n",
                "WHERE cons.constraint_type = 'P'\n",
                "  AND cons.constraint_name = cols.constraint_name\n",
                "  AND cons.owner = cols.owner\n",
                "  AND UPPER(cols.table_name) = '",
                    tableName.ToUpperInvariant(),
                "'"
            ),
            _ => throw new NotSupportedException("Unsupported database type.")
        };

        using DbConnection connection = databaseType switch
        {
            DatabaseType.SqlServer => new SqlConnection(connectionString),
            DatabaseType.Oracle => new OracleConnection(connectionString),
            _ => throw new NotSupportedException("Unsupported database type.")
        };

        connection.Open();

        using DbCommand command = connection.CreateCommand();
        command.CommandText = query;

        using DbDataReader reader = command.ExecuteReader();
        return reader.Read() ? reader["COLUMN_NAME"].ToString() : null;
    }

    public static async IAsyncEnumerable<dynamic> ToDynamicList(DbDataReader reader)
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

    public static IEnumerable<dynamic>? ToDynamic(DbDataReader reader)
    {
        if (!reader.Read()) return null;
        dynamic? expandoObject = new ExpandoObject();
        IDictionary<string, object> dictionary = expandoObject;

        for (int i = 0; i < reader.FieldCount; i++)
        {
            dictionary[reader.GetName(i)] = reader.GetValue(i);
        }

        return expandoObject;

    }


}