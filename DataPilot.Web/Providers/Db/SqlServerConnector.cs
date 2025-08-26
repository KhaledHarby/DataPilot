using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace DataPilot.Web.Providers.Db;

public class SqlServerConnector : IDbConnector
{
    public DataPilot.Web.Data.DbKind Kind => DataPilot.Web.Data.DbKind.SqlServer;

    public async Task TestAsync(string connectionString)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        // do a lightweight ping
        await using var cmd = new SqlCommand("SELECT 1", conn);
        _ = await cmd.ExecuteScalarAsync();
    }

    public async Task<SchemaSnapshot> ReadSchemaAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        var tables = new System.Collections.Generic.List<SchemaTableDto>();
        var dict = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SchemaColumnDto>>();
        {
            const string sql = @"SELECT t.TABLE_SCHEMA, t.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.IS_NULLABLE
FROM INFORMATION_SCHEMA.TABLES t
JOIN INFORMATION_SCHEMA.COLUMNS c ON c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME
WHERE t.TABLE_TYPE IN ('BASE TABLE','VIEW')";
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var schema = reader.GetString(0);
                var table = reader.GetString(1);
                var column = reader.GetString(2);
                var type = reader.GetString(3);
                var isNull = string.Equals(reader.GetString(4), "YES", StringComparison.OrdinalIgnoreCase);
                var key = $"{schema}.{table}";
                if (!dict.TryGetValue(key, out var list))
                {
                    list = new();
                    dict[key] = list;
                }
                list.Add(new SchemaColumnDto(column, type, isNull));
            }
        }
        foreach (var kv in dict)
        {
            tables.Add(new SchemaTableDto(kv.Key, kv.Value));
        }
        // Relations via foreign keys
        const string relSql = @"SELECT 
    tp.name AS PK_TABLE,
    cp.name AS PK_COLUMN,
    tr.name AS FK_TABLE,
    cr.name AS FK_COLUMN
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.tables tr ON tr.object_id = fk.parent_object_id
JOIN sys.columns cr ON cr.object_id = tr.object_id AND cr.column_id = fkc.parent_column_id
JOIN sys.tables tp ON tp.object_id = fk.referenced_object_id
JOIN sys.columns cp ON cp.object_id = tp.object_id AND cp.column_id = fkc.referenced_column_id";
        var relations = new System.Collections.Generic.List<SchemaRelationDto>();
        await using (var relCmd = new SqlCommand(relSql, conn))
        {
            await using var relReader = await relCmd.ExecuteReaderAsync(ct);
            while (await relReader.ReadAsync(ct))
            {
                var pkTable = relReader.GetString(0);
                var pkCol = relReader.GetString(1);
                var fkTable = relReader.GetString(2);
                var fkCol = relReader.GetString(3);
                relations.Add(new SchemaRelationDto(fkTable, fkCol, pkTable, pkCol));
            }
        }
        return new SchemaSnapshot(tables, relations);
    }

    public async Task<QueryResult> ExecuteAsync(string connectionString, string query, QueryOptions options, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(query, conn)
        {
            CommandTimeout = options.TimeoutSeconds
        };
        var sw = Stopwatch.StartNew();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var table = new DataTable();
        table.Load(reader);
        sw.Stop();
        return new QueryResult(table, table.Rows.Count, sw.ElapsedMilliseconds);
    }
}


