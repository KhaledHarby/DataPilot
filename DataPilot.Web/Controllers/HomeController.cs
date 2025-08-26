using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DataPilot.Web.Models;

namespace DataPilot.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Dashboard");
    public IActionResult Privacy() => View();
}
