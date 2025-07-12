using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Signal9.Web.Models;
using Signal9.Web.Services;

namespace Signal9.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IDashboardService _dashboardService;

    public HomeController(
        ILogger<HomeController> logger,
        IDashboardService dashboardService)
    {
        _logger = logger;
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(Guid? tenantId = null, bool useExampleData = true)
    {
        try
        {
            var dashboardModel = await _dashboardService.GetDashboardDataAsync(tenantId, useExampleData);
            ViewBag.UseExampleData = useExampleData;
            return View(dashboardModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            
            // Fallback to example data on error
            var fallbackModel = await _dashboardService.GetDashboardDataAsync(tenantId, true);
            ViewBag.UseExampleData = true;
            ViewBag.ErrorMessage = "Unable to load real data. Showing example data.";
            return View(fallbackModel);
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
