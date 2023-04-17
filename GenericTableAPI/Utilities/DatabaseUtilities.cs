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

        if (connectionString.Contains("SERVER = (") && connectionString.Contains("(HOST="))
        {
            return DatabaseType.Oracle;
        }

        if (connectionString.Contains("SERVER=") && connectionString.Contains("DATABASE="))
        {
            return DatabaseType.SqlServer;
        }

        return DatabaseType.Unknown;
    }

    public static string GetPrimaryKeyColumnName(string? connectionString, string tableName, DatabaseType databaseType)
    {
        string query = databaseType switch
        {
            DatabaseType.SqlServer => $@"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1
                AND TABLE_NAME = '{tableName}'",
            DatabaseType.Oracle => $@"
                SELECT cols.column_name
                FROM all_constraints cons, all_cons_columns cols
                WHERE cons.constraint_type = 'P'
                AND cons.constraint_name = cols.constraint_name
                AND cons.owner = cols.owner
                AND cols.table_name = '{tableName.ToUpper()}'",
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
            dynamic obj = new ExpandoObject();
            IDictionary<string, object> objAsDictionary = obj;

            for (int i = 0; i < reader.FieldCount; i++)
            {
                objAsDictionary[reader.GetName(i)] = reader.GetValue(i);
            }

            yield return obj;
        }
    }

    public static IEnumerable<dynamic>? ToDynamic(DbDataReader reader)
    {
        if (!reader.Read()) return null;
        dynamic? obj = new ExpandoObject();
        IDictionary<string, object> objAsDictionary = obj;

        for (int i = 0; i < reader.FieldCount; i++)
        {
            objAsDictionary[reader.GetName(i)] = reader.GetValue(i);
        }

        return obj;

    }


}