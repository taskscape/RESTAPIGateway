using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Globalization;
using System.Text;

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
    public static string? GetPrimaryKeyColumnName(
    string? connectionString,
    string tableName,
    DatabaseType databaseType)
    {
        var sb = new StringBuilder();
        switch (databaseType)
        {
            case DatabaseType.SqlServer:
                sb.AppendLine("SELECT COLUMN_NAME");
                sb.AppendLine("  FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE");
                sb.AppendLine(" WHERE OBJECTPROPERTY(" +
                              "OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME)," +
                              " 'IsPrimaryKey') = 1");
                sb.Append("   AND TABLE_NAME = @tableName");
                break;

            case DatabaseType.Oracle:
                sb.AppendLine("SELECT cols.column_name");
                sb.AppendLine("  FROM all_constraints cons");
                sb.AppendLine("       JOIN all_cons_columns cols");
                sb.AppendLine("         ON cons.constraint_name = cols.constraint_name");
                sb.AppendLine("        AND cons.owner           = cols.owner");
                sb.AppendLine(" WHERE cons.constraint_type = 'P'");
                sb.Append("   AND UPPER(cols.table_name) = :tableName");
                break;

            default:
                throw new NotSupportedException("Unsupported database type.");
        }

        var sql = sb.ToString();

        // 3) Create and open connection
        using DbConnection connection = databaseType switch
        {
            DatabaseType.SqlServer => new SqlConnection(connectionString),
            DatabaseType.Oracle => new OracleConnection(connectionString),
            _ => throw new NotSupportedException()
        };
        connection.Open();

        // 4) Create command, assign text & parameter
        using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;

        var param = command.CreateParameter();
        param.ParameterName = databaseType == DatabaseType.SqlServer
                              ? "@tableName"
                              : ":tableName";
        param.DbType = DbType.String;
        param.Value = databaseType == DatabaseType.Oracle
                              ? tableName.ToUpperInvariant()
                              : tableName;
        command.Parameters.Add(param);

        // 5) Execute and return
        using DbDataReader reader = command.ExecuteReader();
        return reader.Read()
             ? reader["COLUMN_NAME"]?.ToString()
             : null;
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