using GenericTableAPI.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using static GenericTableAPI.Utilities.DatabaseUtilities;

namespace GenericTableAPI.Services
{
    public class DatabaseSyntaxService
    {
        public string GetAllQuery(string tableName, string? where = null, string? orderBy = null, int? limit = null, string? connectionString = null)
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

            DatabaseType dbType = DatabaseUtilities.GetDatabaseType(connectionString);
            string sql = $"SELECT";

            switch (dbType)
            {
                case DatabaseType.SqlServer:
                    if (limit.HasValue)
                    {
                        sql += $" TOP {limit.Value} ";
                    }
                    sql += $" * FROM {tableName}";
                    if (!string.IsNullOrEmpty(where))
                    {
                        sql += $" WHERE {where}";
                    }
                    break;

                case DatabaseType.Oracle:
                    sql = $" * FROM {tableName}";
                    if (!string.IsNullOrEmpty(where))
                    {
                        sql += $" WHERE {where}";
                        if (limit.HasValue)
                        {
                            sql += $" AND ROWNUM <= {limit.Value}";
                        }
                    }
                    else if (limit.HasValue)
                    {
                        sql += $" WHERE ROWNUM <= {limit.Value}";
                    }
                    break;

                default:
                    throw new NotSupportedException("Unknown database type.");
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                sql += $" ORDER BY {orderBy}";
            }

            return sql;
        }

        public string GetByIdQuery(string tableName, [FromRoute] string id, string? primaryKeyColumn = null)
        {
            if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            if (string.IsNullOrEmpty(tableName) || !Regex.IsMatch(tableName, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            string sql = $"SELECT * FROM {tableName} WHERE {primaryKeyColumn} = '{id}'";

            return sql;
        }

        public string AddQuery(string tableName, IDictionary<string, object> values, string? columns = null, string? strValues = null, string? primaryKeyColumn = null, string? connectionString = null)
        {
            if (string.IsNullOrEmpty(tableName) || !Regex.IsMatch(tableName, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            DatabaseType dbType = DatabaseUtilities.GetDatabaseType(connectionString);

            string sql = "";

            switch (dbType)
            {
                case DatabaseType.SqlServer:
                    sql = $"INSERT INTO {tableName} ({columns}) OUTPUT Inserted.{primaryKeyColumn} VALUES ({strValues})";
                    break;

                case DatabaseType.Oracle:
                    string sequenceName = $"{tableName}_SEQ";
                    sql = $"INSERT INTO {tableName} ({columns}) VALUES ({strValues}); SELECT {sequenceName}.CURRVAL FROM DUAL";
                    break;
                default:
                    throw new NotSupportedException("Unknown database type.");
            }
            return sql;
        }

        public string UpdateQuery(string tableName, string id, IDictionary<string, object> values, string? primaryKeyColumn = null, string? setClauses = null)
        {
            if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            if (string.IsNullOrEmpty(tableName) || !Regex.IsMatch(tableName, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            string sql = $"UPDATE {tableName} SET {setClauses} WHERE {primaryKeyColumn} = {id}";

            return sql;
        }

        public string DeleteQuery(string tableName, string id, string primaryKeyColumn = null)
        {
            if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            if (string.IsNullOrEmpty(tableName) || !Regex.IsMatch(tableName, @"^\w+$"))
            {
                throw new ArgumentException("Invalid table name");
            }

            string sql = $"DELETE FROM {tableName} WHERE {primaryKeyColumn} = '{id}'";

            return sql;
        }
    }
}
