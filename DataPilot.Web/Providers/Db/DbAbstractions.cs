using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DataPilot.Web.Providers.Db;

public record SchemaSnapshot(System.Collections.Generic.List<SchemaTableDto> Tables, System.Collections.Generic.List<SchemaRelationDto>? Relations = null);
public record SchemaTableDto(string Name, System.Collections.Generic.List<SchemaColumnDto> Columns);
public record SchemaColumnDto(string Name, string DataType, bool IsNullable);
public record SchemaRelationDto(string FromTable, string FromColumn, string ToTable, string ToColumn);
public record QueryResult(DataTable Table, int RowCount, long DurationMs);
public record QueryOptions(int TimeoutSeconds = 30, int MaxRows = 1000);

public interface IDbConnector
{
    Task TestAsync(string connectionString);
    Task<SchemaSnapshot> ReadSchemaAsync(string connectionString, CancellationToken ct = default);
    Task<QueryResult> ExecuteAsync(string connectionString, string query, QueryOptions options, CancellationToken ct = default);
    DataPilot.Web.Data.DbKind Kind { get; }
}

public interface IDbConnectorFactory
{
    IDbConnector Create(DataPilot.Web.Data.DbKind kind);
}
