# DataPilot - AI-Powered Database Query Assistant

DataPilot is a modern ASP.NET MVC web application that transforms natural language into database queries using Large Language Models (LLMs). It provides an intuitive interface for database exploration, schema management, and AI-assisted query generation.

## 🚀 Features

### Core Functionality
- **Natural Language to SQL**: Convert plain English queries into database-specific SQL
- **Multi-Database Support**: SQL Server, MySQL, Oracle, MongoDB
- **Schema Discovery**: Automatic scanning and storage of database schemas
- **Connection Management**: Secure storage and management of database connections
- **Query Execution**: Safe, read-only query execution with result visualization
- **Query History**: Track and review past queries with performance metrics

### AI Integration
- **Multi-LLM Support**: OpenAI, Azure OpenAI, Claude, Gemini, Ollama
- **Context-Aware Prompts**: Enhanced prompts with database schema and metadata
- **Dialect-Specific SQL**: Generate database-specific SQL syntax
- **Safety Guards**: Prevent DDL/DML operations and enforce query limits

### Advanced Features
- **Query Enhancer**: Advanced prompt building with schema selection
- **Column Metadata**: Add business context to database columns
- **Schema Relations**: Include foreign key relationships in prompts
- **Dashboard Analytics**: Visual insights into database usage and performance
- **Export Capabilities**: CSV export for query results
- **MCP Server**: Model Context Protocol server for enhanced LLM interactions
- **MCP Dashboard**: Interactive dashboard for MCP server management

## 🏗️ Architecture

### Technology Stack
- **Framework**: ASP.NET Core MVC (.NET 8)
- **Database**: Entity Framework Core with SQL Server (MetaDb)
- **Data Access**: Dapper for user database connections
- **Security**: ASP.NET Core Data Protection API
- **Logging**: Serilog
- **UI**: Bootstrap 5, Font Awesome, Chart.js, Monaco Editor

### Project Structure
```
DataPilot.Web/
├── Controllers/           # MVC Controllers
├── Data/                 # Entity Framework entities and context
├── Providers/            # Database and LLM provider abstractions
│   ├── Db/              # Database connectors
│   └── Llm/             # LLM client implementations
├── Services/             # Core business logic services
├── Extensions/           # Utility extensions
├── Views/                # Razor views
├── Prompts/              # LLM system prompts
└── Properties/           # Application properties
```

### Key Components

#### Database Connectors
- **IDbConnector**: Abstract interface for database operations
- **SqlServerConnector**: SQL Server implementation
- **DbConnectorFactory**: Factory pattern for dynamic connector creation

#### LLM Clients
- **ILLMClient**: Abstract interface for LLM operations
- **OpenAiClient**: OpenAI API integration
- **AzureOpenAIClient**: Azure OpenAI integration
- **OllamaClient**: Local Ollama integration
- **LlmClientFactory**: Factory pattern for LLM client creation

#### Core Services
- **CryptoService**: Connection string encryption/decryption
- **SchemaService**: Database schema discovery and management
- **QueryService**: Query execution and result processing

## 📋 Prerequisites

### System Requirements
- Windows 10/11 or Windows Server 2019+
- .NET 8.0 SDK
- SQL Server (for MetaDb)
- Internet connection (for LLM APIs)

### Required Accounts
- **OpenAI**: API key for GPT models
- **Azure OpenAI**: Endpoint and API key
- **Anthropic**: API key for Claude models
- **Google**: API key for Gemini models
- **Ollama**: Local installation (optional)

## 🛠️ Installation & Setup

### 1. Clone and Build
```bash
git clone <repository-url>
cd DataPilot.Web
dotnet restore
dotnet build
```

### 2. Database Setup
```bash
# Create and apply Entity Framework migrations
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Configuration
Create `appsettings.Development.json` or use User Secrets:

```json
{
  "ConnectionStrings": {
    "MetaDb": "Server=your-server;Database=DataPilot;User ID=your-username;Password=your-password;Pooling=true;Min Pool Size=20;Max Pool Size=200;Connect Timeout=30;TrustServerCertificate=True;"
  },
  "LLM": {
    "DefaultProvider": "OpenAI",
    "Model": "gpt-4o-mini",
    "Temperature": 0.1,
    "MaxTokens": 2000,
    "Providers": {
      "OpenAI": {
        "ApiKey": "your-openai-api-key-here"
      },
      "Claude": {
        "ApiKey": "your-claude-api-key-here"
      },
      "Gemini": {
        "ApiKey": "your-gemini-api-key-here"
      },
      "Azure": {
        "Endpoint": "your-azure-endpoint",
        "ApiKey": "your-azure-api-key",
        "Deployment": "your-deployment-name",
        "ApiVersion": "2024-02-15-preview"
      },
      "Ollama": {
        "BaseUrl": "http://localhost:11434"
      }
    }
  },
  "Auth": {
    "DevBypass": true
  },
  "Mcp": {
    "Name": "DataPilot MCP Server",
    "Version": "1.0.0",
    "Description": "DataPilot Model Context Protocol Server",
    "ServerUrl": "http://localhost:3000",
    "EnableResources": true,
    "EnableTools": true
  }
}
```

### 4. Run Application
```bash
dotnet run
```

Access the application at `https://localhost:5001` or `http://localhost:5000`

## 📖 Usage Guide

### 1. Dashboard
The dashboard provides an overview of:
- Database connections and their status
- Recent query history with performance metrics
- Database distribution charts
- Quick access to key features

### 2. Managing Database Connections

#### Adding a Connection
1. Navigate to **Connections** → **Add New**
2. Select database type (SQL Server, MySQL, Oracle, MongoDB)
3. Enter connection details
4. Test the connection
5. Select tables/views to include in schema
6. Save the connection

#### Connection Security
- Connection strings are encrypted using Windows Data Protection API
- Stored securely in the MetaDb
- Accessible only to authenticated users

### 3. Natural Language Queries

#### Basic Chat Interface
1. Go to **Chat** page
2. Select your database connection
3. Choose LLM provider and model
4. Type your query in natural language
5. Click "Generate SQL" to create the query
6. Click "Run Query" to execute and view results

#### Example Queries
- "Show me all users who registered in the last 30 days"
- "Find customers with more than 5 orders"
- "Get the top 10 products by sales volume"
- "List employees and their departments"

### 4. Query Enhancer

#### Advanced Prompt Building
1. Navigate to **Query Enhancer**
2. Select database connection
3. Choose specific tables and columns
4. Add custom context or business rules
5. Enter your query request
6. Generate enhanced SQL with rich context

#### Column Metadata
- Add display names for user-friendly column references
- Include business descriptions for better AI understanding
- Metadata is saved and reused in future queries

### 5. Schema Management

#### Schema Discovery
- Automatic scanning of tables, views, and columns
- Foreign key relationship detection
- Data type and constraint information
- Nullable/required field identification

#### Schema Enhancement
- Add business context to tables and columns
- Improve AI query generation accuracy
- Maintain consistent terminology

### 6. MCP (Model Context Protocol) Server

#### MCP Dashboard
1. Navigate to **MCP Dashboard** from the main menu
2. View server information and capabilities
3. Explore available resources (database schema, connections, query history)
4. Test available tools (list_connections, get_schema, execute_query, etc.)
5. Connect to external MCP servers
6. Execute MCP tools with custom arguments

#### MCP Features
- **Resources**: Access database schema, connection info, and query history
- **Tools**: Execute database operations through MCP protocol
- **External Connections**: Connect to other MCP-compatible servers
- **Tool Execution**: Run MCP tools with JSON arguments
- **Resource Viewer**: View resource content in modal dialogs

## 🔧 Configuration

### Database Providers

#### SQL Server
```json
{
  "Provider": "SqlServer",
  "ConnectionString": "Server=server;Database=db;Trusted_Connection=true;"
}
```

#### MySQL
```json
{
  "Provider": "MySQL",
  "ConnectionString": "Server=localhost;Database=mydb;Uid=user;Pwd=password;"
}
```

#### Oracle
```json
{
  "Provider": "Oracle",
  "ConnectionString": "Data Source=localhost:1521/orcl;User Id=user;Password=password;"
}
```

#### MongoDB
```json
{
  "Provider": "MongoDB",
  "ConnectionString": "mongodb://localhost:27017/database"
}
```

### LLM Configuration

#### OpenAI
```json
{
  "LLM": {
    "DefaultProvider": "OpenAI",
    "Model": "gpt-4o-mini",
    "Providers": {
      "OpenAI": {
        "ApiKey": "sk-..."
      }
    }
  }
}
```

#### Azure OpenAI
```json
{
  "LLM": {
    "DefaultProvider": "Azure",
    "Model": "gpt-4",
    "Providers": {
      "Azure": {
        "Endpoint": "https://your-resource.openai.azure.com/",
        "ApiKey": "your-key",
        "Deployment": "gpt-4"
      }
    }
  }
}
```

#### Ollama (Local)
```json
{
  "LLM": {
    "DefaultProvider": "Ollama",
    "Model": "llama3.1",
    "Providers": {
      "Ollama": {
        "BaseUrl": "http://localhost:11434"
      }
    }
  }
}
```

## 🔒 Security Features

### Data Protection
- Connection strings encrypted using Windows DPAPI
- User authentication and authorization
- Query execution safety guards
- Read-only query enforcement

### Query Safety
- DDL/DML operation prevention
- Query result limits (default: 100 rows)
- SQL injection protection
- Connection timeout management

## 📊 Monitoring & Logging

### Query Analytics
- Execution time tracking
- Row count monitoring
- Query success/failure rates
- Performance metrics dashboard

### Logging
- Serilog integration
- Structured logging
- Error tracking and debugging
- Audit trail for database operations

## 🚀 Performance Optimization

### Connection Pooling
- Configurable pool sizes
- Connection reuse
- Timeout management
- Resource cleanup

### Caching
- Schema metadata caching
- Query result caching
- LLM response optimization

## 🔧 Troubleshooting

### Common Issues

#### Connection Timeouts
```
Error: Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool.
```
**Solution**: Increase connection pool size in `appsettings.json`:
```json
"ConnectionStrings": {
  "MetaDb": "...;Min Pool Size=20;Max Pool Size=200;Connect Timeout=30;Pooling=true;"
}
```

#### LLM API Errors
```
Error: Response status code does not indicate success: 401 (Unauthorized)
```
**Solution**: Verify API keys are correctly configured in user secrets or environment variables.

#### DataReader Already Open
```
Error: There is already an open DataReader associated with this Connection
```
**Solution**: This has been fixed in the SqlServerConnector with proper resource management.

### Debug Mode
Enable detailed logging in `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "DataPilot.Web": "Debug"
    }
  }
}
```

## 🔄 API Reference

### Controllers

#### ChatController
- `GET /Chat` - Main chat interface
- `POST /Chat/GenerateSql` - Generate SQL from natural language
- `POST /Chat/RunSql` - Execute SQL query
- `GET /Chat/QueryEnhancer` - Advanced query builder
- `POST /Chat/GetTables` - Get tables for connection
- `POST /Chat/GetColumns` - Get columns for table
- `POST /Chat/SaveColumnMetadata` - Save column metadata

#### McpController
- `GET /mcp` - MCP Dashboard UI
- `GET /mcp/info` - Get MCP server information
- `POST /mcp/list_resources` - List available resources
- `POST /mcp/list_tools` - List available tools
- `POST /mcp/read_resource` - Read specific resource content
- `POST /mcp/call_tool` - Execute MCP tool
- `POST /mcp/connect` - Connect to external MCP server

#### ConnectionsController
- `GET /Connections` - List connections
- `GET /Connections/Create` - Create connection form
- `POST /Connections/Create` - Save new connection
- `GET /Connections/Edit/{id}` - Edit connection form
- `POST /Connections/Edit/{id}` - Update connection
- `GET /Connections/Delete/{id}` - Delete confirmation
- `POST /Connections/Delete/{id}` - Delete connection

#### DashboardController
- `GET /Dashboard` - Dashboard overview
- `GET /Dashboard/GetStats` - Dashboard statistics API

### Data Models

#### DbConnectionInfo
```csharp
public class DbConnectionInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DbKind Provider { get; set; }
    public string ConnectionString { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsed { get; set; }
    public bool IsActive { get; set; }
}
```

#### SchemaTable
```csharp
public class SchemaTable
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public string Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool IsView { get; set; }
    public DateTime DiscoveredAt { get; set; }
}
```

#### SchemaColumn
```csharp
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
}
```

## 🎯 Best Practices

### Database Connections
- Use Windows Authentication when possible
- Implement connection pooling
- Set appropriate timeouts
- Regularly test connections

### LLM Usage
- Choose appropriate models for your use case
- Set reasonable temperature values (0.1 for SQL generation)
- Monitor API usage and costs
- Use local models (Ollama) for sensitive data

### Query Generation
- Provide clear, specific natural language queries
- Use the Query Enhancer for complex scenarios
- Add metadata to improve context
- Review generated SQL before execution

### Security
- Regularly rotate API keys
- Use least-privilege database accounts
- Monitor query logs
- Implement proper authentication

## 🔮 Future Enhancements

### Planned Features
- **Query Templates**: Pre-built query patterns
- **Advanced Analytics**: Query performance insights
- **Collaboration**: Share queries and schemas
- **API Endpoints**: REST API for integration
- **Mobile Support**: Responsive mobile interface
- **Enhanced MCP**: Additional MCP tools and resources

### Database Support
- **PostgreSQL**: Native PostgreSQL connector
- **SQLite**: Lightweight database support
- **BigQuery**: Google BigQuery integration
- **Snowflake**: Cloud data warehouse support

### AI Enhancements
- **Query Optimization**: AI-suggested query improvements
- **Schema Recommendations**: AI-powered schema suggestions
- **Natural Language Explanations**: Explain query results in plain English
- **Query Validation**: AI-powered query validation

## 📞 Support

### Getting Help
- Check the troubleshooting section above
- Review application logs for detailed error information
- Ensure all prerequisites are met
- Verify configuration settings

### Contributing
- Follow .NET coding standards
- Add unit tests for new features
- Update documentation for changes
- Test across different database providers

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

**DataPilot** - Making database queries as simple as having a conversation.
