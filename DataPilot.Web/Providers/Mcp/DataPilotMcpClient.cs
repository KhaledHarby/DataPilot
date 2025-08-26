using System.Text;
using System.Text.Json;

namespace DataPilot.Web.Providers.Mcp;

/// <summary>
/// DataPilot MCP Client implementation
/// </summary>
public class DataPilotMcpClient : IMcpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DataPilotMcpClient> _logger;
    private string? _serverUrl;
    private McpClientInfo? _clientInfo;

    public DataPilotMcpClient(HttpClient httpClient, ILogger<DataPilotMcpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<McpInitializeResponseMessage> InitializeAsync(string serverUrl, McpClientInfo clientInfo)
    {
        _serverUrl = serverUrl;
        _clientInfo = clientInfo;

        var message = new McpInitializeMessage
        {
            ClientInfo = clientInfo,
            Capabilities = new McpCapabilities
            {
                Resources = new List<string> { "database_schema", "connection_info", "query_history" },
                Tools = new List<string> { "list_connections", "get_schema", "execute_query", "analyze_query", "get_metadata" }
            }
        };

        var response = await SendMessageAsync<McpInitializeResponseMessage>(message);
        
        _logger.LogInformation("MCP Client initialized with server: {ServerName} v{ServerVersion}", 
            response.ServerInfo.Name, response.ServerInfo.Version);

        return response;
    }

    public async Task<McpListResourcesResponseMessage> ListResourcesAsync()
    {
        var message = new McpListResourcesMessage();
        return await SendMessageAsync<McpListResourcesResponseMessage>(message);
    }

    public async Task<McpReadResourceResponseMessage> ReadResourceAsync(string uri)
    {
        var message = new McpReadResourceMessage
        {
            Uri = uri
        };
        return await SendMessageAsync<McpReadResourceResponseMessage>(message);
    }

    public async Task<McpListToolsResponseMessage> ListToolsAsync()
    {
        var message = new McpListToolsMessage();
        return await SendMessageAsync<McpListToolsResponseMessage>(message);
    }

    public async Task<McpCallToolResponseMessage> CallToolAsync(string name, JsonElement arguments)
    {
        var message = new McpCallToolMessage
        {
            Name = name,
            Arguments = arguments
        };
        return await SendMessageAsync<McpCallToolResponseMessage>(message);
    }

    private async Task<T> SendMessageAsync<T>(McpMessage message) where T : McpMessage
    {
        if (string.IsNullOrEmpty(_serverUrl))
        {
            throw new InvalidOperationException("MCP Client not initialized. Call InitializeAsync first.");
        }

        try
        {
            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            _logger.LogDebug("Sending MCP message to {ServerUrl}: {MessageType}", _serverUrl, message.Type);
            
            var response = await _httpClient.PostAsync(_serverUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"MCP server returned {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize MCP response");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending MCP message {MessageType} to {ServerUrl}", message.Type, _serverUrl);
            throw;
        }
    }
}

/// <summary>
/// MCP Client Factory
/// </summary>
public interface IMcpClientFactory
{
    IMcpClient CreateClient(string serverUrl);
}

/// <summary>
/// MCP Client Factory implementation
/// </summary>
public class McpClientFactory : IMcpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DataPilotMcpClient> _logger;

    public McpClientFactory(IHttpClientFactory httpClientFactory, ILogger<DataPilotMcpClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public IMcpClient CreateClient(string serverUrl)
    {
        var httpClient = _httpClientFactory.CreateClient();
        return new DataPilotMcpClient(httpClient, _logger);
    }
}
