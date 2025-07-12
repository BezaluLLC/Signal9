using Microsoft.AspNetCore.Mvc;
using Signal9.Web.Models;
using Signal9.Web.Services;

namespace Signal9.Web.Controllers;

/// <summary>
/// Dashboard API controller for real-time dashboard data
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get complete dashboard data
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardViewModel>> GetDashboard(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] bool useExampleData = true)
    {
        try
        {
            var dashboard = await _dashboardService.GetDashboardDataAsync(tenantId, useExampleData);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get tenant hierarchy
    /// </summary>
    [HttpGet("tenants")]
    public async Task<ActionResult<List<TenantNodeViewModel>>> GetTenantHierarchy()
    {
        try
        {
            var tenants = await _dashboardService.GetTenantHierarchyAsync();
            return Ok(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant hierarchy");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get agents for a tenant
    /// </summary>
    [HttpGet("agents")]
    public async Task<ActionResult<List<AgentSummaryViewModel>>> GetAgents(
        [FromQuery] Guid? tenantId = null)
    {
        try
        {
            var agents = await _dashboardService.GetAgentsAsync(tenantId);
            return Ok(agents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agents");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsViewModel>> GetStats(
        [FromQuery] Guid? tenantId = null)
    {
        try
        {
            var stats = await _dashboardService.GetDashboardStatsAsync(tenantId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get recent activities
    /// </summary>
    [HttpGet("activities")]
    public async Task<ActionResult<List<RecentActivityViewModel>>> GetActivities(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] int count = 10)
    {
        try
        {
            var activities = await _dashboardService.GetRecentActivitiesAsync(tenantId, count);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activities");
            return StatusCode(500, "Internal server error");
        }
    }
}
