﻿using System.Text;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using GenericTableAPI.Models;
using static GenericTableAPI.Utilities.DatabaseUtilities;

namespace GenericTableAPI.Services
{
    public abstract partial class SyntaxService
    {
        private static readonly Regex TableNameRegex = TableNameRegexInit();
        private static readonly Regex WhereClauseRegex = WhereClauseRegexInit();
        private static readonly Regex OrderByClauseRegex = OrderByClauseRegexInit();
        private const string InvalidTableNameMessage = "Invalid table name";
        private const string InvalidWhereClauseMessage = "Invalid WHERE clause";
        private const string InvalidOrderByClauseMessage = "Invalid ORDER BY clause";

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

        /// <summary>
        /// Validates the table name.
        /// </summary>
        /// <param name="tableName">The table name to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the table name is invalid.</exception>
        private static void ValidateTableName(string? tableName)
        {
            if (string.IsNullOrEmpty(tableName) || !TableNameRegex.IsMatch(tableName))
            {
                throw new ArgumentException(InvalidTableNameMessage);
            }
        }

        /// <summary>
        /// Validates the WHERE clause.
        /// </summary>
        /// <param name="where">The WHERE clause to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the WHERE clause is invalid.</exception>
        private static void ValidateWhereClause(string? where)
        {
            if (!string.IsNullOrEmpty(where) && !WhereClauseRegex.IsMatch(where))
            {
                throw new ArgumentException(InvalidWhereClauseMessage);
            }
        }
        
        /// <summary>
        /// Validates the ORDER BY clause.
        /// </summary>
        /// <param name="orderBy">The ORDER BY clause to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the ORDER BY clause is invalid.</exception>
        private static void ValidateOrderByClause(string? orderBy)
        {
            if (!string.IsNullOrEmpty(orderBy) && !OrderByClauseRegex.IsMatch(orderBy))
            {
                throw new ArgumentException(InvalidOrderByClauseMessage);
            }
        }

        /// <summary>
        /// Constructs a SQL SELECT query to retrieve all records from a table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="schemaName">The name of the schema (optional).</param>
        /// <param name="where">The WHERE clause (optional).</param>
        /// <param name="orderBy">The ORDER BY clause (optional).</param>
        /// <param name="limit">The maximum number of records to retrieve (optional).</param>
        /// <param name="offset">The number of records to skip before starting to return records (optional).</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The SQL SELECT query.</returns>
        public static string GetAllQuery(string tableName, string? schemaName, string? where = null,
            string? orderBy = null, int? limit = null, int? offset = null, string? connectionString = null)
        {
            ValidateTableName(tableName);
            ValidateWhereClause(where);
            ValidateOrderByClause(orderBy);

            DatabaseType dbType = GetDatabaseType(connectionString);
            string query = $"SELECT";
            tableName = GetTableName(tableName, schemaName);

            switch (dbType)
            {
                case DatabaseType.SqlServer:
                    query += " * FROM " + tableName;
                    if (!string.IsNullOrEmpty(where))
                    {
                        query += $" WHERE {where}";
                    }

                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        query += $" ORDER BY {orderBy}";
                    }
                    else
                    {
                        query += " ORDER BY (SELECT NULL)"; // Ensures ORDER BY is present for OFFSET
                    }

                    if (offset.HasValue || limit.HasValue)
                    {
                        query += $" OFFSET {offset.GetValueOrDefault(0)} ROWS";
                        if (limit.HasValue)
                        {
                            query += $" FETCH NEXT {limit.Value} ROWS ONLY";
                        }
                    }
                    break;

                case DatabaseType.Oracle:
                    query += $" * FROM {tableName}";
                    if (!string.IsNullOrEmpty(where))
                    {
                        query += $" WHERE {where}";
                    }

                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        query += $" ORDER BY {orderBy}";
                    }
                    else
                    {
                        query += " ORDER BY NULL"; // Ensures ORDER BY is present for OFFSET
                    }

                    if (offset.HasValue || limit.HasValue)
                    {
                        query += $" OFFSET {offset.GetValueOrDefault(0)} ROWS";
                        if (limit.HasValue)
                        {
                            query += $" FETCH NEXT {limit.Value} ROWS ONLY";
                        }
                    }
                    break;

                case DatabaseType.Unknown:
                default:
                    throw new NotSupportedException("Unknown database type.");
            }

            return query;
        }


        /// <summary>
        /// Constructs a SQL SELECT query to retrieve a record by its primary key.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="schemaName">The name of the schema (optional).</param>
        /// <param name="id">The value of the primary key.</param>
        /// <param name="primaryKeyColumn">The name of the primary key column.</param>
        /// <returns>The SQL SELECT query.</returns>
        public static string GetByIdQuery(string tableName, string? schemaName, [FromRoute] string id, string? primaryKeyColumn = null)
        {
            ValidateTableName(tableName);
            if (string.IsNullOrEmpty(id) || !TableNameRegex.IsMatch(id))
                throw new ArgumentException(InvalidTableNameMessage);

            tableName = GetTableName(tableName, schemaName);

            return $"SELECT * FROM {tableName} WHERE {primaryKeyColumn} = '{id}'"; ;
        }

        /// <summary>
        /// Constructs a SQL INSERT query to add a record to a table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="schemaName">The name of the schema (optional).</param>
        /// <param name="values">A dictionary containing column names and their corresponding values.</param>
        /// <param name="columns">A comma-separated list of column names.</param>
        /// <param name="strValues">A comma-separated list of string representations of values.</param>
        /// <param name="primaryKeyColumn">The name of the primary key column.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The SQL INSERT query.</returns>
        public static string AddQuery(string tableName, string? schemaName, IDictionary<string, object?> values, string? columns = null, string? strValues = null, string? primaryKeyColumn = null, string? connectionString = null)
        {
            ValidateTableName(tableName);

            DatabaseType databaseType = GetDatabaseType(connectionString);

            string sql;
            tableName = GetTableName(tableName, schemaName);

            switch (databaseType)
            {
                case DatabaseType.SqlServer:
                    sql = $"INSERT INTO {tableName} ({columns}) OUTPUT Inserted.{primaryKeyColumn} VALUES ({strValues})";
                    break;

                case DatabaseType.Oracle:
                    sql = $"INSERT INTO {tableName} ({columns}) VALUES ({strValues}) RETURNING ID INTO :ret";
                    break;
                case DatabaseType.Unknown:
                default:
                    throw new NotSupportedException("Unknown database type.");
            }
            return sql;
        }

        /// <summary>
        /// Constructs a SQL UPDATE query to update a record in a table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="schemaName">The name of the schema (optional).</param>
        /// <param name="id">The value of the primary key.</param>
        /// <param name="values">A dictionary containing column names and their corresponding values to update.</param>
        /// <param name="primaryKeyColumn">The name of the primary key column.</param>
        /// <param name="setClauses">A comma-separated list of column name = value pairs for the SET clause.</param>
        /// <returns>The SQL UPDATE query.</returns>
        public static string UpdateQuery(string tableName, string? schemaName, string id, IDictionary<string, object> values, string? primaryKeyColumn = null, string? setClauses = null)
        {
            ValidateTableName(tableName);
            if (string.IsNullOrEmpty(id) || !TableNameRegex.IsMatch(id))
            {
                throw new ArgumentException(InvalidTableNameMessage);
            }

            tableName = GetTableName(tableName, schemaName);

            string sql = $"UPDATE {tableName} SET {setClauses} WHERE {primaryKeyColumn} = '{id}'";

            return sql;
        }

        /// <summary>
        /// Constructs a SQL MERGE query to replace a record in a table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="schemaName">The name of the schema (optional).</param>
        /// <param name="id">The value of the primary key.</param>
        /// <param name="values">A dictionary containing column names and their corresponding values to update.</param>
        /// <param name="connectionString"></param>
        /// <param name="primaryKeyColumn">The name of the primary key column.</param>
        /// <param name="setClauses">A comma-separated list of column name = value pairs for the SET clause.</param>
        /// <returns>The SQL MERGE query.</returns>
        public static string MergeQuery(string tableName, string? schemaName, string id, IDictionary<string, object?> values, string connectionString, string? primaryKeyColumn = null, string? setClauses = null)
        {
            ValidateTableName(tableName);
            if (string.IsNullOrEmpty(id) || !TableNameRegex.IsMatch(id))
            {
                throw new ArgumentException(InvalidTableNameMessage);
            }
            
            DatabaseType databaseType = GetDatabaseType(connectionString: connectionString);
            tableName = GetTableName(tableName, schemaName);
            
            string? keys = primaryKeyColumn;
            if (values?.Count > 0)
            {
                keys = $"{primaryKeyColumn}, {string.Join(", ", values.Keys)}";
            }
            string sql;
            switch (databaseType)
            {
                case DatabaseType.SqlServer:
                    sql = $"MERGE INTO {tableName} AS target USING (SELECT {keys} FROM {tableName} WHERE {primaryKeyColumn} = {id}) AS source ON (target.id = source.id) WHEN MATCHED THEN UPDATE SET {setClauses};";
                    break;

                case DatabaseType.Oracle:
                    sql = $"MERGE INTO {tableName} target USING (SELECT {keys} FROM {tableName} WHERE {primaryKeyColumn} = {id}) source ON (target.id = source.id) WHEN MATCHED THEN UPDATE SET {setClauses}";
                    break;
                case DatabaseType.Unknown:
                default:
                    throw new NotSupportedException("Unknown database type.");
            }

            return sql;
        }

        /// <summary>
        /// Constructs a SQL DELETE query to delete a record from a table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="schemaName">The name of the schema (optional).</param>
        /// <param name="id">The value of the primary key.</param>
        /// <param name="primaryKeyColumn">The name of the primary key column.</param>
        /// <returns>The SQL DELETE query.</returns>
        public static string DeleteQuery(string tableName, string? schemaName, string id, string? primaryKeyColumn = null)
        {
            ValidateTableName(tableName);
            if (string.IsNullOrEmpty(id) || !TableNameRegex.IsMatch(id))
            {
                throw new ArgumentException(InvalidTableNameMessage);
            }

            tableName = GetTableName(tableName, schemaName);

            string sql = $"DELETE FROM {tableName} WHERE {primaryKeyColumn} = '{id}'";

            return sql;
        }
        
        /// <summary>
        /// Executes a stored procedure with the specified parameters and returns the SQL command to execute.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to execute.</param>
        /// <param name="values">A collection of parameters to pass to the stored procedure.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The SQL query to execute the stored procedure.</returns>
        /// <exception cref="NotSupportedException">Thrown when the database type is not supported.</exception>
        public static string ExecuteQuery(string procedureName, IEnumerable<StoredProcedureParameter?>? values, string? connectionString = null)
        {
            DatabaseType databaseType = GetDatabaseType(connectionString);
            
            StringBuilder parameters = new();
            if(values is not null)
            {
                foreach (StoredProcedureParameter? param in values)
                {
                    string parameterFormat;
                    switch (databaseType)
                    {
                        case DatabaseType.SqlServer:
                            parameterFormat = "@{0} = {1}";
                            break;
                        case DatabaseType.Oracle:
                            parameterFormat = "{1}";
                            break;
                        case DatabaseType.Unknown:
                        default:
                            throw new NotSupportedException();
                    }

                    string parameterValue = param?.Type switch
                    {
                        "string" => $"'{param.Value}'",
                        "int" or "float" => param.Value,
                        "null" => "null",
                        _ => throw new NotSupportedException()
                    };
                    parameters.AppendFormat(parameterFormat, param.Name, parameterValue).Append(", ");
                }

                if (parameters.Length > 0)
                {
                    parameters.Length -= 2;
                }
            }

            string sql;
            switch (databaseType)
            {
                case DatabaseType.SqlServer:
                    sql = $"EXEC {procedureName} {parameters}";
                    break;
                case DatabaseType.Oracle:
                    sql = $"EXEC {procedureName}({parameters});";
                    break;
                case DatabaseType.Unknown:
                default:
                    throw new NotSupportedException("Unknown database type.");
            }

            return sql;
        }
        
        /// <summary>
        /// Constructs a SQL SELECT query to retrieve column names from a table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="schemaName">The name of the schema (optional).</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The SQL SELECT query.</returns>
        public static string GetColumnsQuery(string tableName, string? schemaName, string? connectionString = null)
        {
            ValidateTableName(tableName);
            DatabaseType databaseType = GetDatabaseType(connectionString);
            
            tableName = GetTableName(tableName, schemaName);
            string sql;
            switch (databaseType)
            {
                case DatabaseType.SqlServer:
                    sql = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";
                    break;
                case DatabaseType.Oracle:
                    sql = $"SELECT COLUMN_NAME FROM ALL_TAB_COLS WHERE TABLE_NAME = '{tableName}'";
                    break;
                case DatabaseType.Unknown:
                default:
                    throw new NotSupportedException();
            }

            return sql;
        }
        
        [GeneratedRegex(@"^\w+$", RegexOptions.Compiled)]
        private static partial Regex TableNameRegexInit();
        [GeneratedRegex(@"^\w+\s*(=|>=|<=|>|<)\s*.*$", RegexOptions.Compiled)]
        private static partial Regex WhereClauseRegexInit();
        [GeneratedRegex(@"^\w+(\s+(ASC|DESC))?$", RegexOptions.Compiled)]
        private static partial Regex OrderByClauseRegexInit();
    }
}
