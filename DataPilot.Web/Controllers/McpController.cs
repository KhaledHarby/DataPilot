using System.Text.Json;
using DataPilot.Web.Providers.Mcp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace DataPilot.Web.Controllers;

/// <summary>
/// MCP (Model Context Protocol) Controller
/// </summary>
[Route("mcp")]
public class McpController : Controller
{
    private readonly IMcpServer _mcpServer;
    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly ILogger<McpController> _logger;

    public McpController(
        IMcpServer mcpServer,
        IMcpClientFactory mcpClientFactory,
        ILogger<McpController> logger)
    {
        _mcpServer = mcpServer;
        _mcpClientFactory = mcpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// MCP Dashboard UI
    /// </summary>
    [HttpGet]
    [Route("")]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Initialize MCP connection
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize([FromBody] McpInitializeMessage message)
    {
        try
        {
            var response = await _mcpServer.InitializeAsync(message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MCP connection");
            return BadRequest(new McpErrorMessage
            {
                Error = "initialization_failed",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// List available resources
    /// </summary>
    [HttpPost("list_resources")]
    public async Task<IActionResult> ListResources()
    {
        try
        {
            var message = new McpListResourcesMessage();
            var response = await _mcpServer.ListResourcesAsync(message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing resources");
            return BadRequest(new McpErrorMessage
            {
                Error = "list_resources_failed",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Read a specific resource
    /// </summary>
    [HttpPost("read_resource")]
    public async Task<IActionResult> ReadResource([FromBody] ReadResourceRequest request)
    {
        try
        {
            var message = new McpReadResourceMessage
            {
                Uri = request.Uri ?? string.Empty
            };
            var response = await _mcpServer.ReadResourceAsync(message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading resource");
            return BadRequest(new McpErrorMessage
            {
                Error = "read_resource_failed",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// List available tools
    /// </summary>
    [HttpPost("list_tools")]
    public async Task<IActionResult> ListTools()
    {
        try
        {
            var message = new McpListToolsMessage();
            var response = await _mcpServer.ListToolsAsync(message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing tools");
            return BadRequest(new McpErrorMessage
            {
                Error = "list_tools_failed",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Call a specific tool
    /// </summary>
    [HttpPost("call_tool")]
    public async Task<IActionResult> CallTool([FromBody] CallToolRequest request)
    {
        try
        {
            var message = new McpCallToolMessage
            {
                Name = request.Name ?? string.Empty,
                Arguments = System.Text.Json.JsonSerializer.SerializeToElement(request.Arguments ?? new { })
            };
            var response = await _mcpServer.CallToolAsync(message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling tool");
            return BadRequest(new McpErrorMessage
            {
                Error = "call_tool_failed",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Get server information
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetServerInfo()
    {
        return Ok(new
        {
            name = "DataPilot MCP Server",
            version = "1.0.0",
            description = "DataPilot Model Context Protocol Server",
            protocol_version = "2024-11-05",
            capabilities = new
            {
                resources = new object[] { 
                    new { name = "database_schema", description = "Database schema information" },
                    new { name = "connection_info", description = "Database connection information" },
                    new { name = "query_history", description = "Query execution history" }
                },
                tools = new object[] { 
                    new { name = "list_connections", description = "List available database connections" },
                    new { name = "get_schema", description = "Get database schema for a connection" },
                    new { name = "execute_query", description = "Execute a SQL query" },
                    new { name = "analyze_query", description = "Analyze a SQL query" },
                    new { name = "get_metadata", description = "Get metadata for tables and columns" }
                }
            }
        });
    }

    /// <summary>
    /// Connect to external MCP server
    /// </summary>
    [HttpPost("connect")]
    public async Task<IActionResult> ConnectToExternalServer([FromBody] ConnectToMcpServerRequest request)
    {
        try
        {
            var client = _mcpClientFactory.CreateClient(request.ServerUrl);
            
            var clientInfo = new McpClientInfo
            {
                Name = request.ClientName ?? "DataPilot",
                Version = request.ClientVersion ?? "1.0.0"
            };

            var response = await client.InitializeAsync(request.ServerUrl, clientInfo);
            
            return Ok(new
            {
                success = true,
                serverInfo = response.ServerInfo,
                capabilities = response.Capabilities
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to external MCP server {ServerUrl}", request.ServerUrl);
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Execute tool on external MCP server
    /// </summary>
    [HttpPost("external/call_tool")]
    public async Task<IActionResult> CallExternalTool([FromBody] CallExternalToolRequest request)
    {
        try
        {
            var client = _mcpClientFactory.CreateClient(request.ServerUrl);
            
            // Initialize client if not already done
            var clientInfo = new McpClientInfo
            {
                Name = "DataPilot",
                Version = "1.0.0"
            };
            
            await client.InitializeAsync(request.ServerUrl, clientInfo);
            
            // Call the tool
            var arguments = JsonSerializer.SerializeToElement(request.Arguments);
            var response = await client.CallToolAsync(request.ToolName, arguments);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling external tool {ToolName} on {ServerUrl}", request.ToolName, request.ServerUrl);
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}

/// <summary>
/// Request model for connecting to external MCP server
/// </summary>
public class ConnectToMcpServerRequest
{
    public string ServerUrl { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public string? ClientVersion { get; set; }
}

/// <summary>
/// Request model for calling external MCP tool
/// </summary>
public class CallExternalToolRequest
{
    public string ServerUrl { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public object Arguments { get; set; } = new();
}

/// <summary>
/// Request model for reading a resource
/// </summary>
public class ReadResourceRequest
{
    public string? Uri { get; set; }
}

/// <summary>
/// Request model for calling a tool
/// </summary>
public class CallToolRequest
{
    public string? Name { get; set; }
    public object? Arguments { get; set; }
}
