using System.Text.Json;
using DataPilot.Web.Data;
using DataPilot.Web.Services;
using DataPilot.Web.Providers.Db;
using Microsoft.EntityFrameworkCore;

namespace DataPilot.Web.Providers.Mcp;

/// <summary>
/// DataPilot MCP Server implementation
/// </summary>
public class DataPilotMcpServer : IMcpServer
{
    private readonly DataPilotMetaDbContext _dbContext;
    private readonly SchemaService _schemaService;
    private readonly QueryService _queryService;
    private readonly CryptoService _cryptoService;
    private readonly McpServerConfiguration _configuration;
    private readonly ILogger<DataPilotMcpServer> _logger;

    public DataPilotMcpServer(
        DataPilotMetaDbContext dbContext,
        SchemaService schemaService,
        QueryService queryService,
        CryptoService cryptoService,
        McpServerConfiguration configuration,
        ILogger<DataPilotMcpServer> logger)
    {
        _dbContext = dbContext;
        _schemaService = schemaService;
        _queryService = queryService;
        _cryptoService = cryptoService;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<McpInitializeResponseMessage> InitializeAsync(McpInitializeMessage message)
    {
        _logger.LogInformation("MCP Server initializing with client: {ClientName} v{ClientVersion}", 
            message.ClientInfo.Name, message.ClientInfo.Version);

        var capabilities = new McpCapabilities();
        
        if (_configuration.EnableResources)
        {
            capabilities.Resources.Add("database_schema");
            capabilities.Resources.Add("connection_info");
            capabilities.Resources.Add("query_history");
        }
        
        if (_configuration.EnableTools)
        {
            capabilities.Tools.Add("list_connections");
            capabilities.Tools.Add("get_schema");
            capabilities.Tools.Add("execute_query");
            capabilities.Tools.Add("analyze_query");
            capabilities.Tools.Add("get_metadata");
        }

        var response = new McpInitializeResponseMessage
        {
            Capabilities = capabilities,
            ServerInfo = new McpServerInfo
            {
                Name = _configuration.Name,
                Version = _configuration.Version,
                Description = _configuration.Description
            }
        };

        return Task.FromResult(response);
    }

    public Task<McpListResourcesResponseMessage> ListResourcesAsync(McpListResourcesMessage message)
    {
        var resources = new List<McpResource>();

        // Database schema resource
        resources.Add(new McpResource
        {
            Uri = "datapilot://schema",
            Name = "Database Schema",
            Description = "Complete database schema information including tables, columns, and relationships",
            MimeType = "application/json"
        });

        // Connection info resource
        resources.Add(new McpResource
        {
            Uri = "datapilot://connections",
            Name = "Database Connections",
            Description = "List of configured database connections",
            MimeType = "application/json"
        });

        // Query history resource
        resources.Add(new McpResource
        {
            Uri = "datapilot://queries",
            Name = "Query History",
            Description = "Historical query execution data and performance metrics",
            MimeType = "application/json"
        });

        var response = new McpListResourcesResponseMessage
        {
            Resources = resources
        };

        return Task.FromResult(response);
    }

    public async Task<McpReadResourceResponseMessage> ReadResourceAsync(McpReadResourceMessage message)
    {
        try
        {
            switch (message.Uri)
            {
                case "datapilot://schema":
                    return await GetSchemaResourceAsync();
                
                case "datapilot://connections":
                    return await GetConnectionsResourceAsync();
                
                case "datapilot://queries":
                    return await GetQueriesResourceAsync();
                
                default:
                    throw new ArgumentException($"Unknown resource URI: {message.Uri}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading resource {Uri}", message.Uri);
            throw;
        }
    }

    public Task<McpListToolsResponseMessage> ListToolsAsync(McpListToolsMessage message)
    {
        var tools = new List<McpTool>();

        // List connections tool
        tools.Add(new McpTool
        {
            Name = "list_connections",
            Description = "List all configured database connections",
            InputSchema = JsonSerializer.SerializeToElement(new { })
        });

        // Get schema tool
        tools.Add(new McpTool
        {
            Name = "get_schema",
            Description = "Get schema information for a specific database connection",
            InputSchema = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    connectionId = new { type = "string", description = "Database connection ID" }
                },
                required = new[] { "connectionId" }
            })
        });

        // Execute query tool
        tools.Add(new McpTool
        {
            Name = "execute_query",
            Description = "Execute a SQL query on a database connection",
            InputSchema = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    connectionId = new { type = "string", description = "Database connection ID" },
                    sql = new { type = "string", description = "SQL query to execute" },
                    maxRows = new { type = "integer", description = "Maximum number of rows to return", defaultValue = 100 }
                },
                required = new[] { "connectionId", "sql" }
            })
        });

        // Analyze query tool
        tools.Add(new McpTool
        {
            Name = "analyze_query",
            Description = "Analyze a natural language query and generate SQL",
            InputSchema = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    connectionId = new { type = "string", description = "Database connection ID" },
                    naturalLanguageQuery = new { type = "string", description = "Natural language query" },
                    context = new { type = "string", description = "Additional context for the query" }
                },
                required = new[] { "connectionId", "naturalLanguageQuery" }
            })
        });

        // Get metadata tool
        tools.Add(new McpTool
        {
            Name = "get_metadata",
            Description = "Get metadata for tables and columns",
            InputSchema = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    connectionId = new { type = "string", description = "Database connection ID" },
                    tableName = new { type = "string", description = "Table name (optional)" }
                },
                required = new[] { "connectionId" }
            })
        });

        var response = new McpListToolsResponseMessage
        {
            Tools = tools
        };

        return Task.FromResult(response);
    }

    public async Task<McpCallToolResponseMessage> CallToolAsync(McpCallToolMessage message)
    {
        try
        {
            switch (message.Name)
            {
                case "list_connections":
                    return await ListConnectionsAsync(message.Arguments);
                
                case "get_schema":
                    return await GetSchemaAsync(message.Arguments);
                
                case "execute_query":
                    return await ExecuteQueryAsync(message.Arguments);
                
                case "analyze_query":
                    return await AnalyzeQueryAsync(message.Arguments);
                
                case "get_metadata":
                    return await GetMetadataAsync(message.Arguments);
                
                default:
                    return new McpCallToolResponseMessage
                    {
                        Name = message.Name,
                        IsError = true,
                        Error = $"Unknown tool: {message.Name}",
                        Result = JsonSerializer.SerializeToElement(new { })
                    };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling tool {ToolName}", message.Name);
            return new McpCallToolResponseMessage
            {
                Name = message.Name,
                IsError = true,
                Error = ex.Message,
                Result = JsonSerializer.SerializeToElement(new { })
            };
        }
    }

    private async Task<McpReadResourceResponseMessage> GetSchemaResourceAsync()
    {
        var connections = await _dbContext.Connections
            .Include(c => c.Tables)
            .ThenInclude(t => t.Columns)
            .ToListAsync();

        var schemaData = connections.Select(c => new
        {
            connectionId = c.Id,
            name = c.Name,
            provider = c.Provider.ToString(),
            tables = c.Tables.Select(t => new
            {
                name = t.Name,
                displayName = t.DisplayName,
                description = t.Description,
                isView = false, // SchemaTable doesn't have IsView property
                columns = t.Columns.Select(col => new
                {
                    name = col.Name,
                    dataType = col.DataType,
                    isNullable = col.IsNullable,
                    displayName = col.DisplayName,
                    description = col.Description
                }).ToList()
            }).ToList()
        }).ToList();

        return new McpReadResourceResponseMessage
        {
            Uri = "datapilot://schema",
            MimeType = "application/json",
            Contents = JsonSerializer.SerializeToElement(schemaData)
        };
    }

    private async Task<McpReadResourceResponseMessage> GetConnectionsResourceAsync()
    {
        var connections = await _dbContext.Connections
            .Select(c => new
            {
                id = c.Id,
                name = c.Name,
                provider = c.Provider.ToString(),
                isHealthy = c.IsHealthy,
                lastHealth = c.LastHealth,
                createdAt = c.CreatedAt
            })
            .ToListAsync();

        return new McpReadResourceResponseMessage
        {
            Uri = "datapilot://connections",
            MimeType = "application/json",
            Contents = JsonSerializer.SerializeToElement(connections)
        };
    }

    private async Task<McpReadResourceResponseMessage> GetQueriesResourceAsync()
    {
        var queries = await _dbContext.QueryHistories
            .Include(q => q.Connection)
            .OrderByDescending(q => q.ExecutedAt)
            .Take(100)
            .Select(q => new
            {
                id = q.Id,
                connectionId = q.ConnectionId,
                connectionName = q.Connection.Name,
                prompt = q.Prompt,
                sqlText = q.SqlText,
                durationMs = q.DurationMs,
                rowCount = q.RowCount,
                executedAt = q.ExecutedAt
            })
            .ToListAsync();

        return new McpReadResourceResponseMessage
        {
            Uri = "datapilot://queries",
            MimeType = "application/json",
            Contents = JsonSerializer.SerializeToElement(queries)
        };
    }

    private async Task<McpCallToolResponseMessage> ListConnectionsAsync(JsonElement arguments)
    {
        var connections = await _dbContext.Connections
            .Select(c => new
            {
                id = c.Id,
                name = c.Name,
                provider = c.Provider.ToString(),
                isHealthy = c.IsHealthy,
                lastHealth = c.LastHealth
            })
            .ToListAsync();

        return new McpCallToolResponseMessage
        {
            Name = "list_connections",
            Result = JsonSerializer.SerializeToElement(connections)
        };
    }

    private async Task<McpCallToolResponseMessage> GetSchemaAsync(JsonElement arguments)
    {
        var connectionId = arguments.GetProperty("connectionId").GetString();
        if (!Guid.TryParse(connectionId, out var connId))
        {
            throw new ArgumentException("Invalid connection ID");
        }

        var connection = await _dbContext.Connections
            .Include(c => c.Tables)
            .ThenInclude(t => t.Columns)
            .FirstOrDefaultAsync(c => c.Id == connId);

        if (connection == null)
        {
            throw new ArgumentException("Connection not found");
        }

        var schema = connection.Tables.Select(t => new
        {
            name = t.Name,
            displayName = t.DisplayName,
            description = t.Description,
            isView = false, // SchemaTable doesn't have IsView property
            columns = t.Columns.Select(col => new
            {
                name = col.Name,
                dataType = col.DataType,
                isNullable = col.IsNullable,
                displayName = col.DisplayName,
                description = col.Description
            }).ToList()
        }).ToList();

        return new McpCallToolResponseMessage
        {
            Name = "get_schema",
            Result = JsonSerializer.SerializeToElement(schema)
        };
    }

    private async Task<McpCallToolResponseMessage> ExecuteQueryAsync(JsonElement arguments)
    {
        var connectionId = arguments.GetProperty("connectionId").GetString();
        var sql = arguments.GetProperty("sql").GetString();
        var maxRows = arguments.TryGetProperty("maxRows", out var maxRowsProp) ? maxRowsProp.GetInt32() : 100;

        if (!Guid.TryParse(connectionId, out var connId))
        {
            throw new ArgumentException("Invalid connection ID");
        }

        var connection = await _dbContext.Connections.FindAsync(connId);
        if (connection == null)
        {
            throw new ArgumentException("Connection not found");
        }

        // Add LIMIT clause if not present
        if (!sql.ToLowerInvariant().Contains("limit") && !sql.ToLowerInvariant().Contains("top"))
        {
            sql = connection.Provider switch
            {
                DbKind.SqlServer => sql.Replace("SELECT", $"SELECT TOP {maxRows}"),
                DbKind.MySql => $"{sql} LIMIT {maxRows}",
                DbKind.Oracle => $"{sql} FETCH FIRST {maxRows} ROWS ONLY",
                _ => sql
            };
        }

        var connectionString = _cryptoService.Decrypt(connection.ConnectionStringEncrypted);
        var result = await _queryService.ExecuteAsync(connectionString, sql, new QueryOptions(), connection.Provider);

        var response = new
        {
            rowCount = result.RowCount,
            durationMs = result.DurationMs,
            data = result.Table
        };

        return new McpCallToolResponseMessage
        {
            Name = "execute_query",
            Result = JsonSerializer.SerializeToElement(response)
        };
    }

    private async Task<McpCallToolResponseMessage> AnalyzeQueryAsync(JsonElement arguments)
    {
        var connectionId = arguments.GetProperty("connectionId").GetString();
        var naturalLanguageQuery = arguments.GetProperty("naturalLanguageQuery").GetString();
        var context = arguments.TryGetProperty("context", out var contextProp) ? contextProp.GetString() : "";

        if (!Guid.TryParse(connectionId, out var connId))
        {
            throw new ArgumentException("Invalid connection ID");
        }

        var connection = await _dbContext.Connections
            .Include(c => c.Tables)
            .ThenInclude(t => t.Columns)
            .FirstOrDefaultAsync(c => c.Id == connId);

        if (connection == null)
        {
            throw new ArgumentException("Connection not found");
        }

        // Build schema context
        var schemaContext = BuildSchemaContext(connection);
        
        // For now, return a simple analysis
        // In a real implementation, this would call an LLM to generate SQL
        var analysis = new
        {
            naturalLanguageQuery,
            context,
            schemaContext,
            suggestedSql = "SELECT * FROM [table] WHERE [condition] LIMIT 100",
            confidence = 0.8,
            reasoning = "Based on the schema and query intent, this is a suggested SQL structure"
        };

        return new McpCallToolResponseMessage
        {
            Name = "analyze_query",
            Result = JsonSerializer.SerializeToElement(analysis)
        };
    }

    private async Task<McpCallToolResponseMessage> GetMetadataAsync(JsonElement arguments)
    {
        var connectionId = arguments.GetProperty("connectionId").GetString();
        var tableName = arguments.TryGetProperty("tableName", out var tableNameProp) ? tableNameProp.GetString() : null;

        if (!Guid.TryParse(connectionId, out var connId))
        {
            throw new ArgumentException("Invalid connection ID");
        }

        var query = _dbContext.Connections
            .Include(c => c.Tables)
            .ThenInclude(t => t.Columns)
            .Where(c => c.Id == connId);

        if (!string.IsNullOrEmpty(tableName))
        {
            query = query.Where(c => c.Tables.Any(t => t.Name == tableName));
        }

        var connection = await query.FirstOrDefaultAsync();
        if (connection == null)
        {
            throw new ArgumentException("Connection not found");
        }

        var metadata = connection.Tables
            .Where(t => string.IsNullOrEmpty(tableName) || t.Name == tableName)
            .Select(t => new
            {
                tableName = t.Name,
                displayName = t.DisplayName,
                description = t.Description,
                isView = false, // SchemaTable doesn't have IsView property
                columns = t.Columns.Select(col => new
                {
                    columnName = col.Name,
                    dataType = col.DataType,
                    isNullable = col.IsNullable,
                    displayName = col.DisplayName,
                    description = col.Description,
                    maxLength = (int?)null, // SchemaColumn doesn't have these properties
                    precision = (int?)null,
                    scale = (int?)null
                }).ToList()
            }).ToList();

        return new McpCallToolResponseMessage
        {
            Name = "get_metadata",
            Result = JsonSerializer.SerializeToElement(metadata)
        };
    }

    private string BuildSchemaContext(DbConnectionInfo connection)
    {
        var schema = connection.Tables.Select(t => new
        {
            table = t.Name,
            displayName = t.DisplayName,
            description = t.Description,
            columns = t.Columns.Select(col => new
            {
                name = col.Name,
                dataType = col.DataType,
                isNullable = col.IsNullable,
                displayName = col.DisplayName,
                description = col.Description
            }).ToList()
        }).ToList();

        return JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
    }
}
