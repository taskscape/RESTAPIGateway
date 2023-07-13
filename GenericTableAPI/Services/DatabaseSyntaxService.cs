using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using static GenericTableAPI.Utilities.DatabaseUtilities;

namespace GenericTableAPI.Services
{
    public class DatabaseSyntaxService
    {
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

        public string GetAllQuery(string tableName, string? schemaName, string? where = null, string? orderBy = null, int? limit = null, string? connectionString = null)
        {
            if (string.IsNullOrEmpty(tableName) || !Regex.IsMatch(tableName, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            if (!string.IsNullOrEmpty(where) && !Regex.IsMatch(where, @"^\w+\s*=.*$"))
            {
                throw new ArgumentException("Invalid WHERE clause");
            }

            if (!string.IsNullOrEmpty(orderBy) && !Regex.IsMatch(orderBy, @"^\w+(\s+(ASC|DESC))?$"))
            {
                throw new ArgumentException("Invalid ORDER BY clause");
            }

            DatabaseType dbType = GetDatabaseType(connectionString);
            string query = $"SELECT";
            tableName = GetTableName(tableName, schemaName);

            switch (dbType)
            {
                case DatabaseType.SqlServer:
                    if (limit.HasValue)
                    {
                        query += $" TOP {limit.Value} ";
                    }
                    query += $" * FROM {tableName}";
                    if (!string.IsNullOrEmpty(where))
                    {
                        query += $" WHERE {where}";
                    }
                    break;

                case DatabaseType.Oracle:
                    query += $" * FROM {tableName}";
                    if (!string.IsNullOrEmpty(where))
                    {
                        query += $" WHERE {where}";
                        if (limit.HasValue)
                        {
                            query += $" AND ROWNUM <= {limit.Value}";
                        }
                    }
                    else if (limit.HasValue)
                    {
                        query += $" WHERE ROWNUM <= {limit.Value}";
                    }
                    break;

                default:
                    throw new NotSupportedException("Unknown database type.");
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                query += $" ORDER BY {orderBy}";
            }

            return query;
        }

        public string GetByIdQuery(string tableName, string? schemaName, [FromRoute] string id, string? primaryKeyColumn = null)
        {
            if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            if (string.IsNullOrEmpty(tableName) || !Regex.IsMatch(tableName, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            tableName = GetTableName(tableName, schemaName);
            string sql = $"SELECT * FROM {tableName} WHERE {primaryKeyColumn} = '{id}'";

            return sql;
        }

        public string AddQuery(string tableName, string? schemaName, IDictionary<string, object?> values, string? columns = null, string? strValues = null, string? primaryKeyColumn = null, string? connectionString = null)
        {
            if (string.IsNullOrEmpty(tableName) || !Regex.IsMatch(tableName, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            DatabaseType databaseType = GetDatabaseType(connectionString);

            string sql;
            tableName = GetTableName(tableName, schemaName);

            switch (databaseType)
            {
                case DatabaseType.SqlServer:
                    sql = $"INSERT INTO {tableName} ({columns}) OUTPUT Inserted.{primaryKeyColumn} VALUES ({strValues})";
                    break;

                case DatabaseType.Oracle:
                    sql = $"DECLARE ret VARCHAR(32); BEGIN INSERT INTO {tableName} ({columns}) VALUES ({strValues}) RETURNING ID INTO ret; DBMS_OUTPUT.PUT_LINE(ret); END;";
                    break;
                default:
                    throw new NotSupportedException("Unknown database type.");
            }
            return sql;
        }

        public string UpdateQuery(string tableName, string? schemaName, string id, IDictionary<string, object> values, string? primaryKeyColumn = null, string? setClauses = null)
        {
            if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            if (string.IsNullOrEmpty(tableName) || !Regex.IsMatch(tableName, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            tableName = GetTableName(tableName, schemaName);

            string sql = $"UPDATE {tableName} SET {setClauses} WHERE {primaryKeyColumn} = '{id}'";

            return sql;
        }

        public string DeleteQuery(string tableName, string? schemaName, string id, string primaryKeyColumn = null)
        {
            if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            if (string.IsNullOrEmpty(tableName) || !Regex.IsMatch(tableName, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            tableName = GetTableName(tableName, schemaName);

            string sql = $"DELETE FROM {tableName} WHERE {primaryKeyColumn} = '{id}'";

            return sql;
        }
    }
}
