using Signal9.Shared.Models;

namespace Signal9.Web.Models;

public class DashboardViewModel
{
    public TenantHierarchyViewModel TenantHierarchy { get; set; } = new();
    public List<AgentSummaryViewModel> Agents { get; set; } = new();
    public DashboardStatsViewModel Stats { get; set; } = new();
    public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
}

public class TenantHierarchyViewModel
{
    public Guid CurrentTenantId { get; set; }
    public string CurrentTenantName { get; set; } = string.Empty;
    public string CurrentTenantType { get; set; } = string.Empty;
    public List<TenantNodeViewModel> TenantTree { get; set; } = new();
    public string HierarchyPath { get; set; } = string.Empty;
}

public class TenantNodeViewModel
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Level { get; set; }
    public int AgentCount { get; set; }
    public int OnlineAgentCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpanded { get; set; } = true;
    public List<TenantNodeViewModel> Children { get; set; } = new();
}

public class AgentSummaryViewModel
{
    public string AgentId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }
    public string Status { get; set; } = string.Empty;
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
}

public class DashboardStatsViewModel
{
    public int TotalAgents { get; set; }
    public int OnlineAgents { get; set; }
    public int OfflineAgents { get; set; }
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int PendingAlerts { get; set; }
    public double AvgCpuUsage { get; set; }
    public double AvgMemoryUsage { get; set; }
}

public class RecentActivityViewModel
{
    public DateTime Timestamp { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
}
