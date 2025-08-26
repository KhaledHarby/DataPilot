# DataPilot Developer Guide

This guide provides technical details for developers working on DataPilot, including architecture patterns, code examples, and development workflows.

## 🏗️ Architecture Overview

### Design Patterns Used

#### Factory Pattern
- **DbConnectorFactory**: Dynamically creates database connectors based on `DbKind`
- **LlmClientFactory**: Resolves LLM clients based on provider configuration

#### Repository Pattern
- Entity Framework Core DbContext for metadata storage
- Dapper for user database operations

#### Service Layer Pattern
- **CryptoService**: Handles connection string encryption/decryption
- **SchemaService**: Manages database schema discovery
- **QueryService**: Executes queries and processes results

### Dependency Injection

```csharp
// Program.cs - Service Registration
builder.Services.AddScoped<IDbConnectorFactory, DbConnectorFactory>();
builder.Services.AddScoped<SqlServerConnector>();
builder.Services.AddScoped<ILLMClient, LlmClientFactory>();
builder.Services.AddScoped<CryptoService>();
builder.Services.AddScoped<SchemaService>();
builder.Services.AddScoped<QueryService>();
```

## 🔧 Development Setup

### Prerequisites
```bash
# Install .NET 8 SDK
winget install Microsoft.DotNet.SDK.8

# Install Entity Framework tools
dotnet tool install --global dotnet-ef

# Install SQL Server (if not using Docker)
# Download from Microsoft website
```

### Local Development
```bash
# Clone and setup
git clone <repository>
cd DataPilot.Web

# Restore packages
dotnet restore

# Create database
dotnet ef database update

# Run application
dotnet run
```

### User Secrets Configuration
```bash
# Initialize user secrets
dotnet user-secrets init

# Add configuration
dotnet user-secrets set "ConnectionStrings:MetaDb" "Server=localhost;Database=DataPilotMeta;Trusted_Connection=true;TrustServerCertificate=true;"
dotnet user-secrets set "LLM:Providers:OpenAI:ApiKey" "your-api-key"
dotnet user-secrets set "LLM:Providers:Azure:ApiKey" "your-azure-key"
dotnet user-secrets set "LLM:Providers:Azure:Endpoint" "https://your-resource.openai.azure.com/"
```

## 📁 Project Structure

### Key Files and Their Purpose

```
DataPilot.Web/
├── Controllers/
│   ├── ChatController.cs          # Main chat and query generation logic
│   ├── ConnectionsController.cs   # Database connection CRUD operations
│   └── DashboardController.cs     # Dashboard statistics and overview
├── Data/
│   ├── Entities.cs               # EF Core entity definitions
│   └── DataPilotMetaDbContext.cs # EF Core DbContext
├── Providers/
│   ├── Db/
│   │   ├── DbAbstractions.cs     # Database connector interfaces
│   │   ├── DbConnectorFactory.cs # Factory for creating connectors
│   │   └── SqlServerConnector.cs # SQL Server implementation
│   └── Llm/
│       ├── LlmAbstractions.cs    # LLM client interfaces
│       ├── LlmClientFactory.cs   # Factory for creating LLM clients
│       ├── OpenAiClient.cs       # OpenAI API client
│       ├── AzureOpenAIClient.cs  # Azure OpenAI client
│       └── OllamaClient.cs       # Local Ollama client
├── Services/
│   └── CoreServices.cs           # Core business logic services
├── Extensions/
│   └── DataTableExtensions.cs    # Utility extensions
└── Views/
    ├── Chat/
    │   ├── Index.cshtml          # Main chat interface
    │   └── QueryEnhancer.cshtml  # Advanced query builder
    ├── Connections/
    │   ├── Index.cshtml          # Connection list
    │   ├── Create.cshtml         # Add connection form
    │   └── SelectTables.cshtml   # Table selection interface
    └── Dashboard/
        └── Index.cshtml          # Dashboard overview
```

## 🔌 Adding New Database Providers

### 1. Create Connector Implementation

```csharp
// Providers/Db/MySqlConnector.cs
public class MySqlConnector : IDbConnector
{
    private readonly string _connectionString;

    public MySqlConnector(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> TestAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new MySqlCommand("SELECT 1", connection);
        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    public async Task<SchemaSnapshot> ReadSchemaAsync()
    {
        // Implementation for MySQL schema reading
        // Query information_schema.tables, information_schema.columns, etc.
    }
}
```

### 2. Register in Factory

```csharp
// Providers/Db/DbConnectorFactory.cs
public DbConnectorFactory(IServiceProvider serviceProvider)
{
    _serviceProvider = serviceProvider;
    _connectorTypes = new Dictionary<DataPilot.Web.Data.DbKind, Type>
    {
        { DataPilot.Web.Data.DbKind.SqlServer, typeof(SqlServerConnector) },
        { DataPilot.Web.Data.DbKind.MySQL, typeof(MySqlConnector) }, // Add this
        // Add other connectors
    };
}
```

### 3. Add to DbKind Enum

```csharp
// Data/Entities.cs
public enum DbKind
{
    SqlServer,
    MySQL, // Add this
    Oracle,
    MongoDB
}
```

## 🤖 Adding New LLM Providers

### 1. Create Client Implementation

```csharp
// Providers/Llm/ClaudeClient.cs
public class ClaudeClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ClaudeClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<string> GenerateAsync(string prompt, string model, float temperature = 0.1f, int maxTokens = 2000)
    {
        // Implementation for Claude API
        var request = new
        {
            model = model,
            max_tokens = maxTokens,
            temperature = temperature,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        // Make API call and return response
    }
}
```

### 2. Register in Factory

```csharp
// Providers/Llm/LlmClientFactory.cs
public ILLMClient Create(LlmProvider provider)
{
    var http = _httpClientFactory.CreateClient("llm");

    switch (provider)
    {
        case LlmProvider.Claude: // Add this case
            var claudeKey = _cfg["LLM:Providers:Claude:ApiKey"] ?? string.Empty;
            return new ClaudeClient(http, claudeKey);
        // Other cases...
    }
}
```

### 3. Add to LlmProvider Enum

```csharp
// Providers/Llm/LlmAbstractions.cs
public enum LlmProvider
{
    OpenAI,
    Claude, // Add this
    Gemini,
    Ollama,
    Azure
}
```

## 🔐 Security Implementation

### Connection String Encryption

```csharp
// Services/CoreServices.cs
public class CryptoService
{
    public string Encrypt(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    public string Decrypt(string encryptedText)
    {
        var bytes = Convert.FromBase64String(encryptedText);
        var decrypted = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }
}
```

### Query Safety Guards

```csharp
// Services/CoreServices.cs
public class QueryService
{
    private bool IsSafeQuery(string sql)
    {
        var lowerSql = sql.ToLowerInvariant();
        
        // Prevent DDL/DML operations
        var dangerousKeywords = new[] { "insert", "update", "delete", "drop", "create", "alter", "truncate" };
        if (dangerousKeywords.Any(keyword => lowerSql.Contains(keyword)))
            return false;
            
        return true;
    }
}
```

## 📊 Data Models

### Entity Framework Entities

```csharp
// Data/Entities.cs
public class DbConnectionInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DbKind Provider { get; set; }
    public string ConnectionString { get; set; } // Encrypted
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsed { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation properties
    public virtual ICollection<SchemaTable> Tables { get; set; }
}

public class SchemaTable
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public string Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool IsView { get; set; }
    public DateTime DiscoveredAt { get; set; }
    
    // Navigation properties
    public virtual DbConnectionInfo Connection { get; set; }
    public virtual ICollection<SchemaColumn> Columns { get; set; }
}

public class SchemaColumn
{
    public Guid Id { get; set; }
    public Guid TableId { get; set; }
    public string Name { get; set; }
    public string DataType { get; set; }
    public bool IsNullable { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    
    // Navigation properties
    public virtual SchemaTable Table { get; set; }
}
```

## 🔄 API Endpoints

### Chat Controller Endpoints

```csharp
// Controllers/ChatController.cs
[HttpPost]
public async Task<IActionResult> GenerateSql(string prompt, string provider, string model, Guid connectionId)
{
    // Generate SQL from natural language
}

[HttpPost]
public async Task<IActionResult> RunSql(string sql, Guid connectionId)
{
    // Execute SQL query and return results
}

[HttpGet]
public IActionResult QueryEnhancer()
{
    // Advanced query builder interface
}

[HttpPost]
public async Task<IActionResult> GetTables(Guid connectionId)
{
    // Get tables for a connection
}

[HttpPost]
public async Task<IActionResult> GetColumns(Guid tableId)
{
    // Get columns for a table
}

[HttpPost]
public async Task<IActionResult> SaveColumnMetadata(Guid columnId, string displayName, string description)
{
    // Save column metadata
}
```

## 🎨 Frontend Development

### JavaScript Patterns

#### AJAX Calls
```javascript
// Example AJAX call pattern
async function makeApiCall(url, data) {
    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams(data)
        });
        return await response.json();
    } catch (error) {
        console.error('API call failed:', error);
        throw error;
    }
}
```

#### Event Handling
```javascript
// Example event handler pattern
document.getElementById('btnGenerate').addEventListener('click', async function() {
    this.disabled = true;
    this.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Generating...';
    
    try {
        // API call logic
    } catch (error) {
        // Error handling
    } finally {
        this.disabled = false;
        this.innerHTML = '<i class="fas fa-magic me-1"></i>Generate SQL';
    }
});
```

### CSS Patterns

#### Utility Classes
```css
/* Common utility classes used throughout the app */
.bg-gradient-primary {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.font-monospace {
    font-family: 'Courier New', monospace;
}

.schema-item {
    cursor: pointer;
    transition: background-color 0.2s ease;
}

.schema-item:hover {
    background-color: #f8f9fa;
}

.schema-item.selected {
    background-color: #e3f2fd;
    border-color: #2196f3;
}
```

## 🧪 Testing

### Unit Testing Setup

```csharp
// Tests/Unit/ChatControllerTests.cs
[TestClass]
public class ChatControllerTests
{
    private Mock<ISchemaService> _mockSchemaService;
    private Mock<ILLMClient> _mockLlmClient;
    private ChatController _controller;

    [TestInitialize]
    public void Setup()
    {
        _mockSchemaService = new Mock<ISchemaService>();
        _mockLlmClient = new Mock<ILLMClient>();
        _controller = new ChatController(_mockSchemaService.Object, _mockLlmClient.Object);
    }

    [TestMethod]
    public async Task GenerateSql_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var prompt = "Show me all users";
        var connectionId = Guid.NewGuid();
        
        _mockSchemaService.Setup(x => x.ReadSchemaAsync(connectionId))
            .ReturnsAsync(new SchemaSnapshot { /* test data */ });
        
        _mockLlmClient.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("SELECT * FROM Users");

        // Act
        var result = await _controller.GenerateSql(prompt, "OpenAI", "gpt-4", connectionId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(JsonResult));
        // Additional assertions...
    }
}
```

### Integration Testing

```csharp
// Tests/Integration/ChatControllerIntegrationTests.cs
[TestClass]
public class ChatControllerIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        {"ConnectionStrings:MetaDb", "Server=(localdb)\\mssqllocaldb;Database=DataPilotTest;Trusted_Connection=true;"},
                        {"LLM:Providers:OpenAI:ApiKey", "test-key"}
                    });
                });
            });
        _client = _factory.CreateClient();
    }

    [TestMethod]
    public async Task Chat_Index_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/Chat");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.AreEqual("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
    }
}
```

## 🚀 Performance Optimization

### Database Optimization

```csharp
// Optimized schema reading with proper resource management
public async Task<SchemaSnapshot> ReadSchemaAsync()
{
    var snapshot = new SchemaSnapshot();
    
    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();
    
    // Use separate readers to avoid "DataReader already open" error
    using (var command = new SqlCommand("SELECT * FROM sys.tables", connection))
    using (var reader = await command.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync())
        {
            snapshot.Tables.Add(new SchemaTableDto
            {
                Name = reader.GetString("name"),
                IsView = false
            });
        }
    }
    
    // Additional schema reading...
    
    return snapshot;
}
```

### Caching Strategy

```csharp
// Example caching implementation
public class CachedSchemaService : ISchemaService
{
    private readonly ISchemaService _schemaService;
    private readonly IMemoryCache _cache;
    
    public async Task<SchemaSnapshot> ReadSchemaAsync(Guid connectionId)
    {
        var cacheKey = $"schema_{connectionId}";
        
        if (_cache.TryGetValue(cacheKey, out SchemaSnapshot cachedSchema))
        {
            return cachedSchema;
        }
        
        var schema = await _schemaService.ReadSchemaAsync(connectionId);
        _cache.Set(cacheKey, schema, TimeSpan.FromMinutes(30));
        
        return schema;
    }
}
```

## 🔍 Debugging

### Logging Configuration

```json
// appsettings.Development.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/datapilot-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ]
  }
}
```

### Debug Mode Features

```csharp
// Enable detailed logging in development
if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    
    // Add custom debug middleware
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Request: {Method} {Path}", context.Request.Method, context.Request.Path);
        await next();
    });
}
```

## 📦 Deployment

### Docker Support

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DataPilot.Web.csproj", "./"]
RUN dotnet restore "DataPilot.Web.csproj"
COPY . .
RUN dotnet build "DataPilot.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DataPilot.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DataPilot.Web.dll"]
```

### Environment Configuration

```bash
# Production environment variables
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__MetaDb="Server=prod-sql;Database=DataPilotMeta;..."
export LLM__Providers__OpenAI__ApiKey="prod-api-key"
export LLM__Providers__Azure__ApiKey="prod-azure-key"
```

## 🔄 Continuous Integration

### GitHub Actions Example

```yaml
# .github/workflows/ci.yml
name: CI/CD Pipeline

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
    
    - name: Publish
      run: dotnet publish -c Release -o ./publish
```

## 📚 Additional Resources

### Useful Commands

```bash
# Database migrations
dotnet ef migrations add MigrationName
dotnet ef database update
dotnet ef migrations remove

# Package management
dotnet add package PackageName
dotnet remove package PackageName
dotnet list package

# Development tools
dotnet watch run
dotnet user-secrets list
dotnet user-secrets set "Key" "Value"

# Testing
dotnet test --filter "Category=Unit"
dotnet test --logger "console;verbosity=detailed"
```

### Code Style Guidelines

- Use `async/await` for all I/O operations
- Implement proper resource disposal with `using` statements
- Follow C# naming conventions (PascalCase for public members)
- Use nullable reference types where appropriate
- Implement proper error handling and logging
- Write unit tests for business logic
- Use dependency injection for service dependencies

---

This developer guide provides the technical foundation for extending and maintaining DataPilot. For additional support, refer to the main README.md or create an issue in the project repository.
