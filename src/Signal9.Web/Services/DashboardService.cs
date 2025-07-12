using Microsoft.Extensions.Options;
using Signal9.Shared.Configuration;
using Signal9.Shared.Models;
using Signal9.Web.Models;
using System.Text.Json;

namespace Signal9.Web.Services;

/// <summary>
/// Dashboard service implementation with Azure Functions integration
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DashboardService> _logger;
    private readonly string _agentFunctionsUrl;
    private readonly string _webFunctionsUrl;

    public DashboardService(
        HttpClient httpClient,
        ILogger<DashboardService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _agentFunctionsUrl = configuration["AgentFunctionsUrl"] ?? "https://localhost:7071";
        _webFunctionsUrl = configuration["WebFunctionsUrl"] ?? "https://localhost:7072";
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(Guid? tenantId = null, bool useExampleData = true)
    {
        try
        {
            if (useExampleData)
            {
                return CreateSampleDashboardData();
            }

            // Get real data from Azure Functions
            var tenantHierarchy = await GetTenantHierarchyAsync();
            var agents = await GetAgentsAsync(tenantId);
            var stats = await GetDashboardStatsAsync(tenantId);
            var activities = await GetRecentActivitiesAsync(tenantId);

            var currentTenant = tenantHierarchy.FirstOrDefault();
            
            return new DashboardViewModel
            {
                TenantHierarchy = new TenantHierarchyViewModel
                {
                    CurrentTenantId = currentTenant?.TenantId ?? Guid.Empty,
                    CurrentTenantName = currentTenant?.Name ?? "No Tenants",
                    CurrentTenantType = currentTenant?.Type ?? "Organization",
                    TenantTree = tenantHierarchy,
                    HierarchyPath = BuildHierarchyPath(currentTenant)
                },
                Agents = agents,
                Stats = stats,
                RecentActivities = activities
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data");
            
            // Fallback to example data on error
            return CreateSampleDashboardData();
        }
    }

    public async Task<List<TenantNodeViewModel>> GetTenantHierarchyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_webFunctionsUrl}/api/tenants/hierarchy");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var tenants = JsonSerializer.Deserialize<List<Tenant>>(json, GetJsonOptions());
                return BuildTenantHierarchy(tenants ?? new List<Tenant>());
            }
            
            _logger.LogWarning("Failed to get tenant hierarchy: {StatusCode}", response.StatusCode);
            return new List<TenantNodeViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant hierarchy");
            return new List<TenantNodeViewModel>();
        }
    }

    public async Task<List<AgentSummaryViewModel>> GetAgentsAsync(Guid? tenantId = null)
    {
        try
        {
            var url = tenantId.HasValue 
                ? $"{_agentFunctionsUrl}/api/agents?tenantId={tenantId}" 
                : $"{_agentFunctionsUrl}/api/agents";

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var agents = JsonSerializer.Deserialize<List<Agent>>(json, GetJsonOptions());
                return MapToAgentSummary(agents ?? new List<Agent>());
            }
            
            _logger.LogWarning("Failed to get agents: {StatusCode}", response.StatusCode);
            return new List<AgentSummaryViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agents");
            return new List<AgentSummaryViewModel>();
        }
    }

    public async Task<DashboardStatsViewModel> GetDashboardStatsAsync(Guid? tenantId = null)
    {
        try
        {
            var url = tenantId.HasValue 
                ? $"{_webFunctionsUrl}/api/dashboard/stats?tenantId={tenantId}" 
                : $"{_webFunctionsUrl}/api/dashboard/stats";

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var stats = JsonSerializer.Deserialize<DashboardStatsViewModel>(json, GetJsonOptions());
                return stats ?? new DashboardStatsViewModel();
            }
            
            _logger.LogWarning("Failed to get dashboard stats: {StatusCode}", response.StatusCode);
            return new DashboardStatsViewModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return new DashboardStatsViewModel();
        }
    }

    public async Task<List<RecentActivityViewModel>> GetRecentActivitiesAsync(Guid? tenantId = null, int count = 10)
    {
        try
        {
            var url = tenantId.HasValue 
                ? $"{_webFunctionsUrl}/api/activities?tenantId={tenantId}&count={count}" 
                : $"{_webFunctionsUrl}/api/activities?count={count}";

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var activities = JsonSerializer.Deserialize<List<RecentActivityViewModel>>(json, GetJsonOptions());
                return activities ?? new List<RecentActivityViewModel>();
            }
            
            _logger.LogWarning("Failed to get recent activities: {StatusCode}", response.StatusCode);
            return new List<RecentActivityViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activities");
            return new List<RecentActivityViewModel>();
        }
    }

    private List<TenantNodeViewModel> BuildTenantHierarchy(List<Tenant> tenants)
    {
        var tenantLookup = tenants.ToLookup(t => t.ParentTenantId);
        var result = new List<TenantNodeViewModel>();

        void BuildChildren(List<TenantNodeViewModel> nodes, Guid? parentId, int level)
        {
            foreach (var tenant in tenantLookup[parentId])
            {
                var node = new TenantNodeViewModel
                {
                    TenantId = tenant.TenantId,
                    Name = tenant.Name,
                    Type = tenant.TenantType,
                    Level = level,
                    IsActive = tenant.IsActive,
                    // TODO: Get actual agent counts from database
                    AgentCount = 0,
                    OnlineAgentCount = 0
                };

                BuildChildren(node.Children, tenant.TenantId, level + 1);
                nodes.Add(node);
            }
        }

        BuildChildren(result, null, 0);
        return result;
    }

    private List<AgentSummaryViewModel> MapToAgentSummary(List<Agent> agents)
    {
        return agents.Select(agent => new AgentSummaryViewModel
        {
            AgentId = agent.AgentId.ToString(),
            MachineName = agent.MachineName,
            OperatingSystem = agent.OperatingSystem,
            IsOnline = agent.Status == AgentStatus.Online,
            LastSeen = agent.LastSeen,
            Status = agent.Status.ToString(),
            // TODO: Get actual performance metrics from telemetry
            CpuUsage = 0,
            MemoryUsage = 0,
            TenantName = "Unknown", // TODO: Get from tenant lookup
            TenantId = agent.TenantId ?? Guid.Empty
        }).ToList();
    }

    private string BuildHierarchyPath(TenantNodeViewModel? tenant)
    {
        if (tenant == null) return "Root";
        
        // TODO: Build actual hierarchy path from tenant structure
        return $"Root / {tenant.Name}";
    }

    private JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    private DashboardViewModel CreateSampleDashboardData()
    {
        // Sample hierarchical tenant structure
        var tenantTree = new List<TenantNodeViewModel>
        {
            new TenantNodeViewModel
            {
                TenantId = Guid.NewGuid(),
                Name = "Acme Corporation",
                Type = "Organization",
                Level = 0,
                AgentCount = 45,
                OnlineAgentCount = 42,
                IsActive = true,
                Children = new List<TenantNodeViewModel>
                {
                    new TenantNodeViewModel
                    {
                        TenantId = Guid.NewGuid(),
                        Name = "Headquarters",
                        Type = "Site",
                        Level = 1,
                        AgentCount = 25,
                        OnlineAgentCount = 24,
                        IsActive = true,
                        Children = new List<TenantNodeViewModel>
                        {
                            new TenantNodeViewModel
                            {
                                TenantId = Guid.NewGuid(),
                                Name = "IT Department",
                                Type = "Department",
                                Level = 2,
                                AgentCount = 8,
                                OnlineAgentCount = 8,
                                IsActive = true
                            },
                            new TenantNodeViewModel
                            {
                                TenantId = Guid.NewGuid(),
                                Name = "Finance Department",
                                Type = "Department",
                                Level = 2,
                                AgentCount = 12,
                                OnlineAgentCount = 11,
                                IsActive = true
                            }
                        }
                    },
                    new TenantNodeViewModel
                    {
                        TenantId = Guid.NewGuid(),
                        Name = "Branch Office - NYC",
                        Type = "Site",
                        Level = 1,
                        AgentCount = 20,
                        OnlineAgentCount = 18,
                        IsActive = true
                    }
                }
            }
        };

        // Sample agents
        var agents = new List<AgentSummaryViewModel>
        {
            new AgentSummaryViewModel
            {
                AgentId = "DESKTOP-ABC123_a1b2c3d4",
                MachineName = "DESKTOP-ABC123",
                OperatingSystem = "Windows 11 Pro",
                IsOnline = true,
                LastSeen = DateTime.UtcNow.AddMinutes(-2),
                Status = "Healthy",
                CpuUsage = 15.3,
                MemoryUsage = 68.2,
                TenantName = "IT Department"
            },
            new AgentSummaryViewModel
            {
                AgentId = "SERVER-XYZ789_e5f6g7h8",
                MachineName = "SERVER-XYZ789",
                OperatingSystem = "Windows Server 2022",
                IsOnline = true,
                LastSeen = DateTime.UtcNow.AddMinutes(-1),
                Status = "Warning",
                CpuUsage = 85.7,
                MemoryUsage = 92.1,
                TenantName = "Headquarters"
            },
            new AgentSummaryViewModel
            {
                AgentId = "LAPTOP-DEF456_i9j0k1l2",
                MachineName = "LAPTOP-DEF456",
                OperatingSystem = "macOS Sonoma",
                IsOnline = false,
                LastSeen = DateTime.UtcNow.AddHours(-2),
                Status = "Offline",
                CpuUsage = 0,
                MemoryUsage = 0,
                TenantName = "Finance Department"
            }
        };

        return new DashboardViewModel
        {
            TenantHierarchy = new TenantHierarchyViewModel
            {
                CurrentTenantId = tenantTree[0].TenantId,
                CurrentTenantName = "Acme Corporation",
                CurrentTenantType = "Organization",
                TenantTree = tenantTree,
                HierarchyPath = "Root / Acme Corporation"
            },
            Agents = agents,
            Stats = new DashboardStatsViewModel
            {
                TotalAgents = 45,
                OnlineAgents = 42,
                OfflineAgents = 3,
                TotalTenants = 6,
                ActiveTenants = 6,
                PendingAlerts = 3,
                AvgCpuUsage = 33.7,
                AvgMemoryUsage = 53.4
            },
            RecentActivities = new List<RecentActivityViewModel>
            {
                new RecentActivityViewModel
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-5),
                    ActivityType = "Alert",
                    Description = "High CPU usage detected",
                    AgentName = "SERVER-XYZ789",
                    TenantName = "Headquarters",
                    Severity = "Warning"
                },
                new RecentActivityViewModel
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-15),
                    ActivityType = "Agent Connected",
                    Description = "Agent came online",
                    AgentName = "DESKTOP-ABC123",
                    TenantName = "IT Department",
                    Severity = "Info"
                }
            }
        };
    }
}
