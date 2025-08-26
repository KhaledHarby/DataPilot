using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace DataPilot.Web.Controllers;

public class SchemaController : Controller
{
    [HttpGet]
    public IActionResult Enhance()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Enhance(string input)
    {
        ViewData["Before"] = input;
        ViewData["After"] = "[]"; // LLM wiring to be added
        return View();
    }

    [HttpPost]
    public IActionResult Accept()
    {
        TempData["msg"] = "Saved";
        return RedirectToAction(nameof(Enhance));
    }
}
