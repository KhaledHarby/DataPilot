using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataPilot.Web.Providers.Llm;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using DataPilot.Web.Data;
using Microsoft.EntityFrameworkCore;
using System;
using DataPilot.Web.Services;
using DataPilot.Web.Providers.Db;
using Microsoft.AspNetCore.Hosting;
using DataPilot.Web.Extensions;

namespace DataPilot.Web.Controllers;

public class ChatController : Controller
{
    private readonly LlmClientFactory _factory;
    private readonly IConfiguration _cfg;
    private readonly DataPilotMetaDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly QueryService _queryService;
    private readonly CryptoService _crypto;

    public ChatController(LlmClientFactory factory, IConfiguration cfg, DataPilotMetaDbContext db, IWebHostEnvironment env, QueryService queryService, CryptoService crypto)
    {
        _factory = factory;
        _cfg = cfg;
        _db = db;
        _env = env;
        _queryService = queryService;
        _crypto = crypto;
    }

    public async Task<IActionResult> Index()
    {
        var providers = new List<string> { "Ollama", "OpenAI", "Azure" };
        ViewData["Providers"] = providers;
        ViewData["DefaultProvider"] = _cfg["LLM:DefaultProvider"] ?? providers.First();
        ViewData["DefaultModel"] = _cfg["LLM:Model"] ?? "";
        var conns = await _db.Connections.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        ViewData["Connections"] = conns;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> QueryEnhancer()
    {
        var connections = await _db.Connections
            .Include(c => c.Tables)
            .ThenInclude(t => t.Columns)
            .ToListAsync();

        var providers = new List<string> { "Ollama", "OpenAI", "Azure" };
        ViewData["Connections"] = connections;
        ViewData["Providers"] = providers;
        ViewData["DefaultProvider"] = _cfg["LLM:DefaultProvider"] ?? providers.First();
        ViewData["DefaultModel"] = _cfg["LLM:Model"] ?? "";

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GetTables(Guid connectionId)
    {
        var tables = await _db.SchemaTables
            .Where(t => t.ConnectionId == connectionId)
            .Select(t => new { t.Id, t.Name, t.DisplayName, t.Description })
            .ToListAsync();

        return Json(tables);
    }

    [HttpPost]
    public async Task<IActionResult> GetColumns(Guid tableId)
    {
        var columns = await _db.SchemaColumns
            .Where(c => c.TableId == tableId)
            .Select(c => new { c.Id, c.Name, c.DataType, c.IsNullable, c.DisplayName, c.Description })
            .ToListAsync();

        return Json(columns);
    }

    [HttpPost]
    public async Task<IActionResult> SaveColumnMetadata(Guid columnId, string? displayName, string? description)
    {
        try
        {
            var column = await _db.SchemaColumns.FindAsync(columnId);
            if (column == null)
                return Json(new { success = false, error = "Column not found" });

            column.DisplayName = displayName;
            column.Description = description;
            
            await _db.SaveChangesAsync();
            
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveTableMetadata(Guid tableId, string? displayName, string? description)
    {
        try
        {
            var table = await _db.SchemaTables.FindAsync(tableId);
            if (table == null)
                return Json(new { success = false, error = "Table not found" });

            table.DisplayName = displayName;
            table.Description = description;
            
            await _db.SaveChangesAsync();
            
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GenerateEnhancedSql(string prompt, string provider, string model, Guid connectionId, string selectedTables, string selectedColumns, string customContext)
    {
        try
        {
            var conn = await _db.Connections
                .Include(c => c.Tables)
                .ThenInclude(t => t.Columns)
                .FirstOrDefaultAsync(c => c.Id == connectionId);

            if (conn == null)
                return Json(new { success = false, error = "Connection not found" });

            // Parse selected tables and columns
            var tableIds = selectedTables?.Split(',').Where(s => !string.IsNullOrEmpty(s)).Select(Guid.Parse).ToList() ?? new List<Guid>();
            var columnIds = selectedColumns?.Split(',').Where(s => !string.IsNullOrEmpty(s)).Select(Guid.Parse).ToList() ?? new List<Guid>();

            // Build enhanced schema context
            var schemaContext = new System.Text.StringBuilder();
            schemaContext.AppendLine("## SELECTED DATABASE SCHEMA:");
            schemaContext.AppendLine($"Database: {conn.Name} ({conn.Provider})");
            
            if (!string.IsNullOrEmpty(customContext))
            {
                schemaContext.AppendLine($"## CUSTOM CONTEXT:");
                schemaContext.AppendLine(customContext);
                schemaContext.AppendLine();
            }

            if (tableIds.Any())
            {
                schemaContext.AppendLine("## SELECTED TABLES:");
                foreach (var tableId in tableIds)
                {
                    var table = conn.Tables.FirstOrDefault(t => t.Id == tableId);
                    if (table != null)
                    {
                        schemaContext.AppendLine($"### Table: {table.Name}");
                        if (!string.IsNullOrEmpty(table.DisplayName))
                            schemaContext.AppendLine($"Display Name: {table.DisplayName}");
                        if (!string.IsNullOrEmpty(table.Description))
                            schemaContext.AppendLine($"Description: {table.Description}");
                        
                        var tableColumns = columnIds.Any() 
                            ? table.Columns.Where(c => columnIds.Contains(c.Id)).ToList()
                            : table.Columns.ToList();

                        if (tableColumns.Any())
                        {
                            schemaContext.AppendLine("Columns:");
                            foreach (var col in tableColumns)
                            {
                                schemaContext.AppendLine($"  - {col.Name} ({col.DataType}){(col.IsNullable ? " NULL" : " NOT NULL")}");
                                if (!string.IsNullOrEmpty(col.DisplayName))
                                    schemaContext.AppendLine($"    Display: {col.DisplayName}");
                                if (!string.IsNullOrEmpty(col.Description))
                                    schemaContext.AppendLine($"    Description: {col.Description}");
                            }
                        }
                        schemaContext.AppendLine();
                    }
                }
            }

            // Get LLM client
            var llmClient = _factory.Create(Enum.Parse<LlmProvider>(provider));
            if (string.IsNullOrEmpty(model))
            {
                model = provider.ToLower() switch
                {
                    "openai" => "gpt-4o-mini",
                    "azure" => "gpt-4o-mini",
                    "ollama" => "llama3.1",
                    "claude" => "claude-3-haiku-20240307",
                    "gemini" => "gemini-1.5-flash",
                    _ => "gpt-4o-mini"
                };
            }

            // Build enhanced prompt
            var enhancedPrompt = $@"{schemaContext}

## USER REQUEST:
{prompt}

## INSTRUCTIONS:
Based on the selected schema above, generate a SQL query that addresses the user's request. Focus on the selected tables and columns when relevant to the query.

## DATABASE TYPE: {conn.Provider.ToString().ToUpper()}
## RESPONSE FORMAT: SQL query only, no explanations";

            var messages = new List<LlmMessage>
            {
                new LlmMessage("system", await System.IO.File.ReadAllTextAsync("Prompts/sql_generation.md")),
                new LlmMessage("user", enhancedPrompt)
            };

            var response = await llmClient.ChatAsync(messages, new LlmRequestOptions(model));
            var sql = response.Trim();

            // Clean up SQL
            sql = sql.Replace("`", "").Replace("sql", "");

            return Json(new { success = true, sql });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GenerateSql(string prompt, string provider, string model, Guid connectionId)
    {
        var selectedProvider = string.IsNullOrWhiteSpace(provider) ? (_cfg["LLM:DefaultProvider"] ?? "Ollama") : provider;
        var defaultProvider = _cfg["LLM:DefaultProvider"] ?? "Ollama";
        var configuredModel = _cfg["LLM:Model"] ?? string.Empty;
        string selectedModel;
        
        // If user didn't specify a model OR switched provider from default, pick a sensible provider-specific default
        if (string.IsNullOrWhiteSpace(model) || !selectedProvider.Equals(defaultProvider, System.StringComparison.OrdinalIgnoreCase))
        {
            selectedModel = selectedProvider.Equals("OpenAI", System.StringComparison.OrdinalIgnoreCase)
                ? "gpt-4o-mini"
                : selectedProvider.Equals("Ollama", System.StringComparison.OrdinalIgnoreCase)
                    ? "llama3.1"
                    : configuredModel; // Azure uses Deployment name configured
        }
        else
        {
            selectedModel = string.IsNullOrWhiteSpace(model) ? configuredModel : model;
        }
        
        var client = _factory.Create(selectedProvider);

        // Build schema context for selected connection
        string schemaContext = string.Empty;
        string dbDirectives = string.Empty;
        var conn = await _db.Connections.FirstOrDefaultAsync(c => c.Id == connectionId);
        if (conn != null)
        {
            // DB-specific guidance for correct dialect and limits
            switch (conn.Provider)
            {
                case DbKind.SqlServer:
                    dbDirectives = "Database: SQL Server. Use SELECT TOP N ... no LIMIT syntax. If limiting rows, use TOP 100 unless user requests otherwise.";
                    break;
                case DbKind.MySql:
                    dbDirectives = "Database: MySQL. Use LIMIT N at the end for row limits (e.g., LIMIT 100).";
                    break;
                case DbKind.Oracle:
                    dbDirectives = "Database: Oracle. Use FETCH FIRST N ROWS ONLY for limits (e.g., FETCH FIRST 100 ROWS ONLY).";
                    break;
                case DbKind.Mongo:
                    dbDirectives = "Database: MongoDB. Return a JSON aggregation pipeline; include $limit with 100 unless otherwise specified.";
                    break;
            }
            
            var tables = await _db.SchemaTables.Where(t => t.ConnectionId == connectionId)
                .OrderBy(t => t.Name)
                .ToListAsync();
            var tableIds = tables.Select(t => t.Id).ToList();
            var columns = await _db.SchemaColumns.Where(c => tableIds.Contains(c.TableId))
                .OrderBy(c => c.Name)
                .ToListAsync();
            var map = columns.GroupBy(c => c.TableId).ToDictionary(g => g.Key, g => g.ToList());
            var lines = new List<string>();
            lines.Add("SCHEMA START");
            lines.Add($"DB_KIND: {conn.Provider}");
            foreach (var t in tables)
            {
                lines.Add($"TABLE {t.Name}");
                if (map.TryGetValue(t.Id, out var cols))
                {
                    foreach (var col in cols)
                    {
                        lines.Add($"  - {col.Name} {col.DataType}{(col.IsNullable ? " NULL" : " NOT NULL")}");
                    }
                }
            }
            // Relations if any
            var relations = await _db.SchemaTables.Where(t => t.ConnectionId == connectionId)
                .SelectMany(t => _db.SchemaColumns.Where(c => c.TableId == t.Id))
                .Take(0) // placeholder to keep LINQ provider happy - relations are not in meta DB yet
                .ToListAsync();
            if (relations.Count == 0)
            {
                // If we later persist relations, render here
            }
            lines.Add("SCHEMA END");
            schemaContext = string.Join("\n", lines);
        }

        // Load system instruction
        string systemPrompt;
        try
        {
            var path = System.IO.Path.Combine(_env.ContentRootPath, "Prompts", "sql_generation.md");
            systemPrompt = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : "You generate safe, read-only SQL using the provided schema.";
        }
        catch { systemPrompt = "You generate safe, read-only SQL using the provided schema."; }

        var messages = new List<LlmMessage>
        {
            new("system", systemPrompt + "\n\n" + dbDirectives + "\n\n" + schemaContext),
            new("user", prompt)
        };
        var sql = await client.ChatAsync(messages, new LlmRequestOptions(selectedModel));
        sql = sql.Replace("`", "").Replace("sql","");
        
        return Json(new { sql, success = true });
    }

    [HttpPost]
    public async Task<IActionResult> RunSql(string sql, Guid connectionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return Json(new { success = false, error = "SQL query is empty" });
            }

            sql = sql.Replace("`", string.Empty);
            var conn = await _db.Connections.FirstOrDefaultAsync(c => c.Id == connectionId);
            if (conn == null) 
            {
                return Json(new { success = false, error = "Connection not found" });
            }

            var cs = _crypto.Decrypt(conn.ConnectionStringEncrypted);
            var result = await _queryService.ExecuteAsync(cs, sql, new QueryOptions(), conn.Provider);
                 
            // Log the query execution
            var history = new QueryHistory
            {
                ConnectionId = connectionId,
                Prompt = "Manual execution",
                SqlText = sql,
                ExecutedAt = DateTime.UtcNow,
                DurationMs = result.DurationMs,
                RowCount = result.RowCount
            };
            _db.QueryHistories.Add(history);
            await _db.SaveChangesAsync();

            return Json(new { 
                success = true, 
                rowCount = result.RowCount, 
                duration = result.DurationMs,
                data = new { Rows = result.Table.ToDynamicList() }
            });
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            System.Diagnostics.Debug.WriteLine($"RunSql error: {ex}");
            return Json(new { success = false, error = ex.Message });
        }
    }
}
