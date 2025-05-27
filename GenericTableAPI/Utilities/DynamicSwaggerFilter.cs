using GenericTableAPI.Utilities;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class DynamicSwaggerFilter : IDocumentFilter
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DynamicSwaggerFilter> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DynamicSwaggerFilter(IConfiguration configuration, ILogger<DynamicSwaggerFilter> logger, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            return;
        }
        try
        {
            var connection = new DatabaseHandler(_configuration.GetConnectionString("DefaultConnection"));
            var tables = connection.GetTableNames();

            foreach (var table in tables)
            {
                var columns = connection.GetSchemaForTable(table);

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

                var Operations = new Dictionary<OperationType, OpenApiOperation>();
                var OperationsId = new Dictionary<OperationType, OpenApiOperation>();

                if (TableValidationUtility.ValidTablePermission(_configuration, table, "select", user))
                {
                    Operations.Add(OperationType.Get, GetAllOperation(table));
                    OperationsId.Add(OperationType.Get, GetByIdOperation(table));
                }
                if (TableValidationUtility.ValidTablePermission(_configuration, table, "update", user))
                {
                    OperationsId.Add(OperationType.Put, PutOperation(table));
                    OperationsId.Add(OperationType.Patch, PatchOperation(table));
                }
                if (TableValidationUtility.ValidTablePermission(_configuration, table, "insert", user))
                {
                    Operations.Add(OperationType.Post, PostOperation(table));
                }
                if (TableValidationUtility.ValidTablePermission(_configuration, table, "delete", user))
                {
                    OperationsId.Add(OperationType.Delete, DeleteOperation(table));
                }

                swaggerDoc.Paths[string.Concat("/api/tables/", table)] = new OpenApiPathItem
                {
                    Operations = Operations
                };

                swaggerDoc.Paths[string.Concat("/api/tables/", table, "/{id}")] = new OpenApiPathItem
                {
                    Operations = OperationsId
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
    private OpenApiOperation GetAllOperation(string table)
    {
        return new OpenApiOperation
        {
            Summary = string.Concat("Get all records from ", table),
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
                            new OpenApiParameter { Name = "whereParams", In = ParameterLocation.Query, Schema = new OpenApiSchema { Type = "string" }, Description = "Filter parameters" },
                            new OpenApiParameter { Name = "orderBy", In = ParameterLocation.Query, Schema = new OpenApiSchema { Type = "string" }, Description = "Order by column names" },
                            new OpenApiParameter { Name = "limit", In = ParameterLocation.Query, Schema = new OpenApiSchema { Type = "integer", Format = "int32" }, Description = "Limit results" },
                            new OpenApiParameter { Name = "offset", In = ParameterLocation.Query, Schema = new OpenApiSchema { Type = "integer", Format = "int32" }, Description = "Skip records for pagination" }
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
                },
                ["400"] = new OpenApiResponse { Description = "Bad Request" },
                ["404"] = new OpenApiResponse { Description = "Not Found" }
            }
        };
    }

    private OpenApiOperation GetByIdOperation(string table)
    {
        return new OpenApiOperation
        {
            Summary = string.Concat("Get record from ", table, " by ID"),
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
                            new OpenApiParameter { Name = "id", In = ParameterLocation.Path, Schema = new OpenApiSchema { Type = "string" }, Description = "ID of the record to retrieve", Required = true },
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
                ["400"] = new OpenApiResponse { Description = "Bad Request" },
                ["404"] = new OpenApiResponse { Description = "Not Found" }
            }
        };
    }

    private OpenApiOperation PostOperation(string table)
    {
        return new OpenApiOperation
        {
            Summary = string.Concat("Insert a new record into ", table),
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
                ["201"] = new OpenApiResponse
                {
                    Description = "Created",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = table } }
                        }
                    }
                },
                ["400"] = new OpenApiResponse { Description = "Bad Request" }
            }
        };
    }

    private OpenApiOperation PatchOperation(string table)
    {
        return new OpenApiOperation
        {
            Summary = string.Concat("Update a record from ", table, " by ID"),
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
                ["400"] = new OpenApiResponse { Description = "Bad Request" },
                ["404"] = new OpenApiResponse { Description = "Not Found" }
            }
        };
    }

    private OpenApiOperation PutOperation(string table)
    {
        return new OpenApiOperation
        {
            Summary = string.Concat("Replace a record from ", table, " by ID"),
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
                            new OpenApiParameter { Name = "id", In = ParameterLocation.Path, Schema = new OpenApiSchema { Type = "string" }, Description = "ID of the record to replace", Required = true },
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
                ["400"] = new OpenApiResponse { Description = "Bad Request" },
                ["404"] = new OpenApiResponse { Description = "Not Found" }
            }
        };
    }

    private OpenApiOperation DeleteOperation(string table)
    {
        return new OpenApiOperation
        {
            Summary = string.Concat("Delete a record from ", table, " by ID"),
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
                ["200"] = new OpenApiResponse { Description = "Success" },
                ["400"] = new OpenApiResponse { Description = "Bad Request" },
                ["404"] = new OpenApiResponse { Description = "Not Found" }
            }
        };
    }

    private string MapSqlTypeToOpenApiType(string sqlType)
    {
        return sqlType.ToLower() switch
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
            "number" => "number",
            _ => "string"
        };
    }
}