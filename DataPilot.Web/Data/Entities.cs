using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace DataPilot.Web.Data;

public enum DbKind { SqlServer, MySql, Oracle, Mongo }

public class ApplicationUser : IdentityUser
{
}

public class DbConnectionInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DbKind Provider { get; set; }
    public string ConnectionStringEncrypted { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastHealth { get; set; }
    public bool IsHealthy { get; set; }
    public List<SchemaTable> Tables { get; set; } = new();
}

public class SchemaTable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConnectionId { get; set; }
    public DbConnectionInfo? Connection { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public List<SchemaColumn> Columns { get; set; } = new();
}

public class SchemaColumn
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TableId { get; set; }
    public SchemaTable? Table { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? TagsJson { get; set; }
}

public class QueryHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConnectionId { get; set; }
    public DbConnectionInfo? Connection { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string SqlText { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }
    public long DurationMs { get; set; }
    public int RowCount { get; set; }
    public string? ErrorText { get; set; }
}

public class LlmProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double? TopP { get; set; }
    public int MaxTokens { get; set; }
    public bool IsDefault { get; set; }
}


