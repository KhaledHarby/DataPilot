using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DataPilot.Web.Data;
using DataPilot.Web.Providers.Db;

namespace DataPilot.Web.Services;

public class CryptoService
{
    public string Encrypt(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return plain;
        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(bytes);
    }
    public string Decrypt(string cipher)
    {
        if (string.IsNullOrEmpty(cipher)) return cipher;
        var bytes = ProtectedData.Unprotect(Convert.FromBase64String(cipher), null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}

public class SqlSafetyGuard
{
    public bool IsReadOnly(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return false;
        var s = sql.Trim();
        if (s.Contains(";")) return false; // no batching
        var upper = s.ToUpperInvariant();
        if (!(upper.StartsWith("SELECT") || upper.StartsWith("WITH"))) return false;
        string[] banned = new[] { "INSERT", "UPDATE", "DELETE", "MERGE", "DROP", "ALTER", "TRUNCATE", "GRANT", "REVOKE", "EXEC", "CREATE" };
        foreach (var b in banned)
        {
            if (upper.Contains($" {b} ")) return false;
        }
        return true;
    }
}

public class QueryService
{
    private readonly SqlSafetyGuard _guard;
    private readonly IDbConnectorFactory _connectorFactory;

    public QueryService(SqlSafetyGuard guard, IDbConnectorFactory connectorFactory)
    {
        _guard = guard;
        _connectorFactory = connectorFactory;
    }

    public async Task<QueryResult> ExecuteAsync(string connectionString, string query, QueryOptions options, DbKind dbKind, CancellationToken ct = default)
    {
        if (!_guard.IsReadOnly(query)) throw new InvalidOperationException("Unsafe query blocked");
        
        var connector = _connectorFactory.Create(dbKind);
        return await connector.ExecuteAsync(connectionString, query, options, ct);
    }
}

public class SchemaService
{
    private readonly IDbConnectorFactory _connectorFactory;
    private readonly DataPilotMetaDbContext _db;

    public SchemaService(IDbConnectorFactory connectorFactory, DataPilotMetaDbContext db)
    {
        _connectorFactory = connectorFactory;
        _db = db;
    }

    public Task TestAsync(string cs, DbKind dbKind) 
    {
        var connector = _connectorFactory.Create(dbKind);
        return connector.TestAsync(cs);
    }
    
    public Task<SchemaSnapshot> ReadSchemaAsync(string cs, DbKind dbKind, CancellationToken ct = default) 
    {
        var connector = _connectorFactory.Create(dbKind);
        return connector.ReadSchemaAsync(cs, ct);
    }
}
