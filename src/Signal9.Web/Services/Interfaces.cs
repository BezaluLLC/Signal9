using Signal9.Shared.Models;
using Signal9.Web.Models;

namespace Signal9.Web.Services;

/// <summary>
/// Service interface for dashboard operations
/// </summary>
public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync(Guid? tenantId = null, bool useExampleData = true);
    Task<List<TenantNodeViewModel>> GetTenantHierarchyAsync();
    Task<List<AgentSummaryViewModel>> GetAgentsAsync(Guid? tenantId = null);
    Task<DashboardStatsViewModel> GetDashboardStatsAsync(Guid? tenantId = null);
    Task<List<RecentActivityViewModel>> GetRecentActivitiesAsync(Guid? tenantId = null, int count = 10);
}

/// <summary>
/// Service interface for tenant operations
/// </summary>
public interface ITenantService
{
    Task<List<Tenant>> GetTenantsAsync();
    Task<List<Tenant>> GetTenantHierarchyAsync(Guid? parentId = null);
    Task<Tenant?> GetTenantAsync(Guid tenantId);
    Task<Tenant> CreateTenantAsync(Tenant tenant);
    Task<Tenant> UpdateTenantAsync(Tenant tenant);
    Task DeleteTenantAsync(Guid tenantId);
}

/// <summary>
/// Service interface for agent operations
/// </summary>
public interface IAgentService
{
    Task<List<Agent>> GetAgentsAsync(Guid? tenantId = null);
    Task<Agent?> GetAgentAsync(string agentId);
    Task<Agent> UpdateAgentAsync(Agent agent);
    Task DeleteAgentAsync(string agentId);
    Task<bool> SendCommandAsync(string agentId, AgentCommand command);
}
