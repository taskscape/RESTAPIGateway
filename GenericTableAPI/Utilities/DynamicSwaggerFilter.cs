using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class DynamicSwaggerFilter : IDocumentFilter
{
    private readonly IConfiguration _configuration;

    public DynamicSwaggerFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        using var dbConnection = new SqlConnection(connectionString);

        //TODO: ADD FILTRATION BASED ON AUTHORIZATION

        //TODO: GET THE RESPONSES RIGHT (EVERY POSSIBLE RESPONSE CODE ETC.)

        //TODO: TEST IF IT WORKS FOR ORACLE DB

        var tables = dbConnection.Query<string>("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'");

        foreach (var table in tables)
        {
            var columns = dbConnection.Query<(string COLUMN_NAME, string DATA_TYPE)>(
                "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName",
                new { TableName = table }
            );

            var schema = new OpenApiSchema
            {
                Type = "object",
                Properties = columns.ToDictionary(
                    col => col.COLUMN_NAME,
                    col => new OpenApiSchema { Type = MapSqlTypeToOpenApiType(col.DATA_TYPE) }
                )
            };

            context.SchemaGenerator.GenerateSchema(typeof(Dictionary<string, object>), context.SchemaRepository);
            context.SchemaRepository.Schemas[table] = schema;

            swaggerDoc.Paths[$"/api/tables/{table}"] = new OpenApiPathItem
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    [OperationType.Get] = new OpenApiOperation
                    {
                        Summary = $"Get all records from {table}",
                        Security = new List<OpenApiSecurityRequirement>
                        {
                            new OpenApiSecurityRequirement
                            {
                                {
                                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" } },
                                    new List<string>()
                                }
                            }
                        },
                        Parameters = new List<OpenApiParameter>
                        {
                            new OpenApiParameter { Name = "where", In = ParameterLocation.Query, Schema = new OpenApiSchema { Type = "string" }, Description = "Filter condition" },
                            new OpenApiParameter { Name = "orderBy", In = ParameterLocation.Query, Schema = new OpenApiSchema { Type = "string" }, Description = "Order by condition" },
                            new OpenApiParameter { Name = "limit", In = ParameterLocation.Query, Schema = new OpenApiSchema { Type = "integer", Format = "int32" }, Description = "Limit results" }
                        },
                        Responses = new OpenApiResponses
                        {
                            ["200"] = new OpenApiResponse
                            {
                                Description = "Success",
                                Content = new Dictionary<string, OpenApiMediaType>
                                {
                                    ["application/json"] = new OpenApiMediaType
                                    {
                                        Schema = new OpenApiSchema
                                        {
                                            Type = "array",
                                            Items = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = table } }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    [OperationType.Post] = new OpenApiOperation
                    {
                        Summary = $"Insert a new record into {table}",
                        Security = new List<OpenApiSecurityRequirement>
                        {
                            new OpenApiSecurityRequirement
                            {
                                {
                                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" } },
                                    new List<string>()
                                }
                            }
                        },
                        RequestBody = new OpenApiRequestBody
                        {
                            Required = true,
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = table } }
                                }
                            }
                        },
                        Responses = new OpenApiResponses
                        {
                            //TODO: MAKE CORRECT RESPONSES
                            ["201"] = new OpenApiResponse { Description = "Created" },
                            ["400"] = new OpenApiResponse { Description = "Bad Request" }
                        }
                    }
                }
            };

            swaggerDoc.Paths[$"/api/tables/{table}/{{id}}"] = new OpenApiPathItem
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    [OperationType.Get] = new OpenApiOperation
                    {
                        Summary = $"Insert a new record into {table}",
                        Security = new List<OpenApiSecurityRequirement>
                        {
                            new OpenApiSecurityRequirement
                            {
                                {
                                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" } },
                                    new List<string>()
                                }
                            }
                        },
                        Parameters = new List<OpenApiParameter>
                        {
                            new OpenApiParameter { Name = "id", In = ParameterLocation.Path, Schema = new OpenApiSchema { Type = "string" }, Description = "ID of the record to update", Required = true },
                            new OpenApiParameter { Name = "primaryKeyColumnName", In = ParameterLocation.Query, Schema = new OpenApiSchema { Type = "string" }, Description = "Primary Key Column Name" }
                        },
                        Responses = new OpenApiResponses
                        {
                            ["200"] = new OpenApiResponse
                            {
                                Description = "Success",
                                Content = new Dictionary<string, OpenApiMediaType>
                                {
                                    ["application/json"] = new OpenApiMediaType
                                    {
                                        Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = table } }
                                    }
                                }
                            },
                            ["404"] = new OpenApiResponse { Description = "Not Found" }
                        }
                    },
                    [OperationType.Patch] = new OpenApiOperation
                    {
                        Summary = $"Insert a new record into {table}",
                        Security = new List<OpenApiSecurityRequirement>
                        {
                            new OpenApiSecurityRequirement
                            {
                                {
                                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" } },
                                    new List<string>()
                                }
                            }
                        },
                        Parameters = new List<OpenApiParameter>
                        {
                            new OpenApiParameter { Name = "id", In = ParameterLocation.Path, Schema = new OpenApiSchema { Type = "string" }, Description = "ID of the record to update", Required = true },
                            new OpenApiParameter { Name = "primaryKeyColumnName", In = ParameterLocation.Query, Schema = new OpenApiSchema { Type = "string" }, Description = "Primary Key Column Name" }
                        },
                        RequestBody = new OpenApiRequestBody
                        {
                            Required = true,
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = table } }
                                }
                            }
                        },
                        Responses = new OpenApiResponses
                        {
                            //TODO: MAKE CORRECT RESPONSES
                            ["201"] = new OpenApiResponse { Description = "Created" },
                            ["400"] = new OpenApiResponse { Description = "Bad Request" }
                        }
                    },
                    [OperationType.Put] = new OpenApiOperation
                    {
                        Summary = $"Insert a new record into {table}",
                        Security = new List<OpenApiSecurityRequirement>
                        {
                            new OpenApiSecurityRequirement
                            {
                                {
                                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" } },
                                    new List<string>()
                                }
                            }
                        },
                        Parameters = new List<OpenApiParameter>
                        {
                            new OpenApiParameter { Name = "id", In = ParameterLocation.Path, Schema = new OpenApiSchema { Type = "string" }, Description = "ID of the record to update", Required = true },
                            new OpenApiParameter { Name = "primaryKeyColumnName", In = ParameterLocation.Query, Schema = new OpenApiSchema { Type = "string" }, Description = "Primary Key Column Name" }
                        },
                        RequestBody = new OpenApiRequestBody
                        {
                            Required = true,
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = table } }
                                }
                            }
                        },
                        Responses = new OpenApiResponses
                        {
                            //TODO: MAKE CORRECT RESPONSES
                            ["201"] = new OpenApiResponse { Description = "Created" },
                            ["400"] = new OpenApiResponse { Description = "Bad Request" }
                        }
                    },
                    [OperationType.Delete] = new OpenApiOperation
                    {
                        Summary = $"Delete a record from {table} by ID",
                        Security = new List<OpenApiSecurityRequirement>
                        {
                            new OpenApiSecurityRequirement
                            {
                                {
                                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" } },
                                    new List<string>()
                                }
                            }
                        },
                        Parameters = new List<OpenApiParameter>
                        {
                            new OpenApiParameter
                            {
                                Name = "id",
                                In = ParameterLocation.Path,
                                Required = true,
                                Schema = new OpenApiSchema { Type = "string" },
                                Description = "ID of the record to delete"
                            },
                            new OpenApiParameter { Name = "primaryKeyColumnName", In = ParameterLocation.Query, Schema = new OpenApiSchema { Type = "string" }, Description = "Primary Key Column Name" }
                        },
                        Responses = new OpenApiResponses
                        {
                            //TODO: MAKE CORRECT RESPONSES
                            ["204"] = new OpenApiResponse { Description = "No Content" },
                            ["404"] = new OpenApiResponse { Description = "Not Found" }
                        }
                    }
                }
            };
        }
    }

    private string MapSqlTypeToOpenApiType(string sqlType)
    {
        return sqlType switch
        {
            "int" => "integer",
            "bigint" => "integer",
            "smallint" => "integer",
            "tinyint" => "integer",
            "bit" => "boolean",
            "nvarchar" => "string",
            "varchar" => "string",
            "text" => "string",
            "datetime" => "string",
            "date" => "string",
            "decimal" => "number",
            "float" => "number",
            "real" => "number",
            _ => "string"
        };
    }
}
