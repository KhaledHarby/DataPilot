using System.Text.Json;

namespace DataPilot.Web.Providers.Mcp;

/// <summary>
/// MCP (Model Context Protocol) message types
/// </summary>
public static class McpMessageTypes
{
    public const string Initialize = "initialize";
    public const string InitializeResponse = "initialize_response";
    public const string ListResources = "list_resources";
    public const string ListResourcesResponse = "list_resources_response";
    public const string ReadResource = "read_resource";
    public const string ReadResourceResponse = "read_resource_response";
    public const string ListTools = "list_tools";
    public const string ListToolsResponse = "list_tools_response";
    public const string CallTool = "call_tool";
    public const string CallToolResponse = "call_tool_response";
    public const string NotifyResourcesChanged = "notify_resources_changed";
    public const string Error = "error";
}

/// <summary>
/// Base MCP message
/// </summary>
public abstract class McpMessage
{
    public string Type { get; set; } = string.Empty;
    public string? Id { get; set; }
}

/// <summary>
/// MCP Initialize message
/// </summary>
public class McpInitializeMessage : McpMessage
{
    public McpInitializeMessage()
    {
        Type = McpMessageTypes.Initialize;
    }

    public string ProtocolVersion { get; set; } = "2024-11-05";
    public McpCapabilities Capabilities { get; set; } = new();
    public McpClientInfo ClientInfo { get; set; } = new();
}

/// <summary>
/// MCP Initialize Response message
/// </summary>
public class McpInitializeResponseMessage : McpMessage
{
    public McpInitializeResponseMessage()
    {
        Type = McpMessageTypes.InitializeResponse;
    }

    public McpCapabilities Capabilities { get; set; } = new();
    public McpServerInfo ServerInfo { get; set; } = new();
}

/// <summary>
/// MCP List Resources message
/// </summary>
public class McpListResourcesMessage : McpMessage
{
    public McpListResourcesMessage()
    {
        Type = McpMessageTypes.ListResources;
    }
}

/// <summary>
/// MCP List Resources Response message
/// </summary>
public class McpListResourcesResponseMessage : McpMessage
{
    public McpListResourcesResponseMessage()
    {
        Type = McpMessageTypes.ListResourcesResponse;
    }

    public List<McpResource> Resources { get; set; } = new();
}

/// <summary>
/// MCP Read Resource message
/// </summary>
public class McpReadResourceMessage : McpMessage
{
    public McpReadResourceMessage()
    {
        Type = McpMessageTypes.ReadResource;
    }

    public string Uri { get; set; } = string.Empty;
}

/// <summary>
/// MCP Read Resource Response message
/// </summary>
public class McpReadResourceResponseMessage : McpMessage
{
    public McpReadResourceResponseMessage()
    {
        Type = McpMessageTypes.ReadResourceResponse;
    }

    public string Uri { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public JsonElement Contents { get; set; }
}

/// <summary>
/// MCP List Tools message
/// </summary>
public class McpListToolsMessage : McpMessage
{
    public McpListToolsMessage()
    {
        Type = McpMessageTypes.ListTools;
    }
}

/// <summary>
/// MCP List Tools Response message
/// </summary>
public class McpListToolsResponseMessage : McpMessage
{
    public McpListToolsResponseMessage()
    {
        Type = McpMessageTypes.ListToolsResponse;
    }

    public List<McpTool> Tools { get; set; } = new();
}

/// <summary>
/// MCP Call Tool message
/// </summary>
public class McpCallToolMessage : McpMessage
{
    public McpCallToolMessage()
    {
        Type = McpMessageTypes.CallTool;
    }

    public string Name { get; set; } = string.Empty;
    public JsonElement Arguments { get; set; }
}

/// <summary>
/// MCP Call Tool Response message
/// </summary>
public class McpCallToolResponseMessage : McpMessage
{
    public McpCallToolResponseMessage()
    {
        Type = McpMessageTypes.CallToolResponse;
    }

    public string Name { get; set; } = string.Empty;
    public JsonElement Result { get; set; }
    public bool IsError { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// MCP Error message
/// </summary>
public class McpErrorMessage : McpMessage
{
    public McpErrorMessage()
    {
        Type = McpMessageTypes.Error;
    }

    public string Error { get; set; } = string.Empty;
    public string? Message { get; set; }
    public JsonElement? Data { get; set; }
}

/// <summary>
/// MCP Capabilities
/// </summary>
public class McpCapabilities
{
    public List<string> Resources { get; set; } = new();
    public List<string> Tools { get; set; } = new();
}

/// <summary>
/// MCP Client Info
/// </summary>
public class McpClientInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// MCP Server Info
/// </summary>
public class McpServerInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// MCP Resource
/// </summary>
public class McpResource
{
    public string Uri { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
}

/// <summary>
/// MCP Tool
/// </summary>
public class McpTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JsonElement InputSchema { get; set; }
}

/// <summary>
/// MCP Server interface
/// </summary>
public interface IMcpServer
{
    Task<McpInitializeResponseMessage> InitializeAsync(McpInitializeMessage message);
    Task<McpListResourcesResponseMessage> ListResourcesAsync(McpListResourcesMessage message);
    Task<McpReadResourceResponseMessage> ReadResourceAsync(McpReadResourceMessage message);
    Task<McpListToolsResponseMessage> ListToolsAsync(McpListToolsMessage message);
    Task<McpCallToolResponseMessage> CallToolAsync(McpCallToolMessage message);
}

/// <summary>
/// MCP Client interface
/// </summary>
public interface IMcpClient
{
    Task<McpInitializeResponseMessage> InitializeAsync(string serverUrl, McpClientInfo clientInfo);
    Task<McpListResourcesResponseMessage> ListResourcesAsync();
    Task<McpReadResourceResponseMessage> ReadResourceAsync(string uri);
    Task<McpListToolsResponseMessage> ListToolsAsync();
    Task<McpCallToolResponseMessage> CallToolAsync(string name, JsonElement arguments);
}

/// <summary>
/// MCP Server configuration
/// </summary>
public class McpServerConfiguration
{
    public string Name { get; set; } = "DataPilot MCP Server";
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = "DataPilot Model Context Protocol Server";
    public string ServerUrl { get; set; } = "http://localhost:3000";
    public bool EnableResources { get; set; } = true;
    public bool EnableTools { get; set; } = true;
    public List<string> AllowedOrigins { get; set; } = new();
}
