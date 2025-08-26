using System;
using System.Collections.Generic;

namespace DataPilot.Web.Models;

public class TableSelectionItem
{
    public string Name { get; set; } = string.Empty; // schema.name
    public bool Selected { get; set; }
}

public class TableSelectionViewModel
{
    public Guid ConnectionId { get; set; }
    public List<TableSelectionItem> Items { get; set; } = new();
}


