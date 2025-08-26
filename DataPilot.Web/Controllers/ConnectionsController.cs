using System;
using System.Threading.Tasks;
using DataPilot.Web.Data;
using DataPilot.Web.Services;
using DataPilot.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

namespace DataPilot.Web.Controllers;

public class ConnectionsController : Controller
{
    private readonly DataPilotMetaDbContext _db;
    private readonly CryptoService _crypto;
    private readonly SchemaService _schemaService;

    public ConnectionsController(DataPilotMetaDbContext db, CryptoService crypto, SchemaService schemaService)
    {
        _db = db; _crypto = crypto; _schemaService = schemaService;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Connections.AsNoTracking().ToListAsync();
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var conn = await _db.Connections.FirstOrDefaultAsync(x => x.Id == id);
        if (conn == null) return NotFound();
        ViewData["ConnectionStringPlain"] = _crypto.Decrypt(conn.ConnectionStringEncrypted);
        return View(conn);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, DbConnectionInfo model, string connectionStringPlain)
    {
        var conn = await _db.Connections.FirstOrDefaultAsync(x => x.Id == id);
        if (conn == null) return NotFound();
        if (!ModelState.IsValid) { ViewData["ConnectionStringPlain"] = connectionStringPlain; return View(model); }
        try
        {
            await _schemaService.TestAsync(connectionStringPlain, model.Provider);
            conn.Name = model.Name;
            conn.Provider = model.Provider;
            conn.ConnectionStringEncrypted = _crypto.Encrypt(connectionStringPlain);
            conn.IsHealthy = true; conn.LastHealth = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(SelectTables), new { id = conn.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["ConnectionStringPlain"] = connectionStringPlain;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        var conn = await _db.Connections.FirstOrDefaultAsync(x => x.Id == id);
        if (conn == null) return NotFound();
        return View(conn);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var conn = await _db.Connections
            .Include(c => c.Tables)
                .ThenInclude(t => t.Columns)
            .FirstOrDefaultAsync(x => x.Id == id);
            
        if (conn == null) return NotFound();
        
        try
        {
            // Delete related QueryHistory records first (due to Restrict constraint)
            var queryHistories = await _db.QueryHistories.Where(qh => qh.ConnectionId == id).ToListAsync();
            if (queryHistories.Any())
            {
                _db.QueryHistories.RemoveRange(queryHistories);
                await _db.SaveChangesAsync(); // Save changes to clear the constraint
            }
            
            // Delete the connection - cascading deletes will handle SchemaTables and SchemaColumns
            _db.Connections.Remove(conn);
            await _db.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Connection and all related data deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting connection: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new DbConnectionInfo());
    }

    [HttpPost]
    public async Task<IActionResult> Create(DbConnectionInfo model, string connectionStringPlain)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            await _schemaService.TestAsync(connectionStringPlain, model.Provider);
            model.ConnectionStringEncrypted = _crypto.Encrypt(connectionStringPlain);
            model.IsHealthy = true; model.LastHealth = DateTime.UtcNow;
            _db.Connections.Add(model);
            await _db.SaveChangesAsync();
            // read schema now to populate selection screen
            var snap = await _schemaService.ReadSchemaAsync(connectionStringPlain, model.Provider);
            var vm = new TableSelectionViewModel
            {
                ConnectionId = model.Id,
                Items = snap.Tables.ConvertAll(t => new TableSelectionItem { Name = t.Name, Selected = true })
            };
            TempData["_conn_cs_enc"] = model.ConnectionStringEncrypted;
            return RedirectToAction(nameof(SelectTables), new { id = model.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> SelectTables(Guid id)
    {
        var conn = await _db.Connections.FirstOrDefaultAsync(x => x.Id == id);
        if (conn == null) return NotFound();
        var cs = _crypto.Decrypt(conn.ConnectionStringEncrypted);
        var snap = await _schemaService.ReadSchemaAsync(cs, conn.Provider);
        var vm = new TableSelectionViewModel
        {
            ConnectionId = id,
            Items = snap.Tables.ConvertAll(t => new TableSelectionItem { Name = t.Name, Selected = true })
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> SelectTables(TableSelectionViewModel vm)
    {
        var conn = await _db.Connections.FirstOrDefaultAsync(x => x.Id == vm.ConnectionId);
        if (conn == null) return NotFound();
        // Save selected tables as SchemaTable records (create missing, delete unselected)
        var selected = new HashSet<string>(vm.Items.FindAll(i => i.Selected).ConvertAll(i => i.Name));
        var existing = await _db.SchemaTables.Where(t => t.ConnectionId == vm.ConnectionId).ToListAsync();
        // delete removed
        _db.SchemaTables.RemoveRange(existing.FindAll(t => !selected.Contains(t.Name)));
        // add new
        foreach (var name in selected)
        {
            if (!existing.Exists(t => t.Name == name))
            {
                _db.SchemaTables.Add(new SchemaTable { ConnectionId = vm.ConnectionId, Name = name });
            }
        }
        await _db.SaveChangesAsync();

        // Persist columns for selected tables from live schema snapshot
        var cs = _crypto.Decrypt(conn.ConnectionStringEncrypted);
        var snapshot = await _schemaService.ReadSchemaAsync(cs, conn.Provider);
        var nameToColumns = snapshot.Tables.ToDictionary(
            t => t.Name,
            t => t.Columns
        );
        var tables = await _db.SchemaTables.Where(t => t.ConnectionId == vm.ConnectionId && selected.Contains(t.Name)).ToListAsync();
        foreach (var table in tables)
        {
            if (!nameToColumns.TryGetValue(table.Name, out var cols)) continue;
            var oldCols = await _db.SchemaColumns.Where(c => c.TableId == table.Id).ToListAsync();
            _db.SchemaColumns.RemoveRange(oldCols);
            foreach (var c in cols)
            {
                _db.SchemaColumns.Add(new SchemaColumn
                {
                    TableId = table.Id,
                    Name = c.Name,
                    DataType = c.DataType,
                    IsNullable = c.IsNullable
                });
            }
        }
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
