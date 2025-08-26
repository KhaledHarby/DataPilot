using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataPilot.Web.Data;
using DataPilot.Web.Services;
using System.Linq;

namespace DataPilot.Web.Controllers;

public class DashboardController : Controller
{
    private readonly DataPilotMetaDbContext _db;
    private readonly SchemaService _schemaService;

    public DashboardController(DataPilotMetaDbContext db, SchemaService schemaService)
    {
        _db = db;
        _schemaService = schemaService;
    }

    public async Task<IActionResult> Index()
    {
        var dashboardData = new DashboardViewModel
        {
            TotalConnections = await _db.Connections.CountAsync(),
            TotalTables = await _db.SchemaTables.CountAsync(),
            TotalColumns = await _db.SchemaColumns.CountAsync(),
            TotalQueries = await _db.QueryHistories.CountAsync(),
            
            RecentConnections = await _db.Connections
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new ConnectionSummary
                {
                    Id = c.Id,
                    Name = c.Name,
                    Provider = c.Provider.ToString(),
                    CreatedAt = c.CreatedAt,
                    TableCount = _db.SchemaTables.Count(t => t.ConnectionId == c.Id)
                })
                .ToListAsync(),
            
            RecentQueries = await _db.QueryHistories
                .OrderByDescending(q => q.ExecutedAt)
                .Take(10)
                .Select(q => new QuerySummary
                {
                    Id = q.Id,
                    Sql = q.SqlText,
                    ExecutedAt = q.ExecutedAt,
                    DurationMs = (int)q.DurationMs,
                    RowCount = q.RowCount,
                    ConnectionName = _db.Connections
                        .Where(c => c.Id == q.ConnectionId)
                        .Select(c => c.Name)
                        .FirstOrDefault()
                })
                .ToListAsync(),
            
            DatabaseStats = await _db.Connections
                .GroupBy(c => c.Provider)
                .Select(g => new DatabaseStat
                {
                    Provider = g.Key.ToString(),
                    Count = g.Count(),
                    TableCount = _db.SchemaTables
                        .Where(t => _db.Connections
                            .Where(c => c.Provider == g.Key)
                            .Select(c => c.Id)
                            .Contains(t.ConnectionId))
                        .Count()
                })
                .ToListAsync()
        };

        return View(dashboardData);
    }

    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var stats = new
        {
            connections = await _db.Connections.CountAsync(),
            tables = await _db.SchemaTables.CountAsync(),
            columns = await _db.SchemaColumns.CountAsync(),
            queries = await _db.QueryHistories.CountAsync(),
            recentQueries = await _db.QueryHistories
                .OrderByDescending(q => q.ExecutedAt)
                .Take(5)
                .Select(q => new { q.SqlText, q.ExecutedAt, q.DurationMs, q.RowCount })
                .ToListAsync()
        };

        return Json(stats);
    }
}

public class DashboardViewModel
{
    public int TotalConnections { get; set; }
    public int TotalTables { get; set; }
    public int TotalColumns { get; set; }
    public int TotalQueries { get; set; }
    public List<ConnectionSummary> RecentConnections { get; set; } = new();
    public List<QuerySummary> RecentQueries { get; set; } = new();
    public List<DatabaseStat> DatabaseStats { get; set; } = new();
}

public class ConnectionSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Provider { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int TableCount { get; set; }
}

public class QuerySummary
{
    public Guid Id { get; set; }
    public string Sql { get; set; } = "";
    public DateTime ExecutedAt { get; set; }
    public int DurationMs { get; set; }
    public int RowCount { get; set; }
    public string? ConnectionName { get; set; }
}

public class DatabaseStat
{
    public string Provider { get; set; } = "";
    public int Count { get; set; }
    public int TableCount { get; set; }
}
