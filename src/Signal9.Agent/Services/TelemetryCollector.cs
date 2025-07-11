using Microsoft.Extensions.Logging;
using Signal9.Shared.DTOs;
using System.Diagnostics;

namespace Signal9.Agent.Services;

/// <summary>
/// Service for collecting system telemetry data
/// </summary>
public class TelemetryCollector : ITelemetryCollector
{
    private readonly ILogger<TelemetryCollector> _logger;
    private readonly string _agentId;
    private readonly string _tenantCode;

    public TelemetryCollector(ILogger<TelemetryCollector> logger, string agentId = "default", string tenantCode = "default")
    {
        _logger = logger;
        _agentId = agentId;
        _tenantCode = tenantCode;
    }

    public async Task<TelemetryDto> CollectTelemetryAsync()
    {
        try
        {
            var telemetry = new TelemetryDto
            {
                AgentId = _agentId,
                TenantCode = _tenantCode,
                Timestamp = DateTime.UtcNow,
                CpuUsage = await GetCpuUsageAsync(),
                MemoryUsage = await GetMemoryUsageAsync(),
                DiskUsage = await GetDiskUsageAsync(),
                NetworkIn = await GetNetworkInAsync(),
                NetworkOut = await GetNetworkOutAsync(),
                SystemInfo = await GetSystemInfoAsync(),
                CustomMetrics = new Dictionary<string, object>
                {
                    { "CollectionTime", DateTime.UtcNow },
                    { "AgentVersion", "1.0.0" },
                    { "ProcessCount", Process.GetProcesses().Length }
                }
            };

            return telemetry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting telemetry");
            return new TelemetryDto 
            { 
                AgentId = _agentId,
                TenantCode = _tenantCode,
                Timestamp = DateTime.UtcNow,
                CustomMetrics = new Dictionary<string, object>
                {
                    { "Error", ex.Message }
                }
            };
        }
    }

    public async Task<TelemetryDto> CollectOnDemandTelemetryAsync(string metricType)
    {
        try
        {
            var telemetry = new TelemetryDto
            {
                AgentId = _agentId,
                TenantCode = _tenantCode,
                Timestamp = DateTime.UtcNow,
                CustomMetrics = new Dictionary<string, object>
                {
                    { "MetricType", metricType },
                    { "OnDemand", true }
                }
            };

            switch (metricType.ToLower())
            {
                case "cpu":
                    telemetry.CpuUsage = await GetCpuUsageAsync();
                    break;
                case "memory":
                    telemetry.MemoryUsage = await GetMemoryUsageAsync();
                    break;
                case "disk":
                    telemetry.DiskUsage = await GetDiskUsageAsync();
                    break;
                case "network":
                    telemetry.NetworkIn = await GetNetworkInAsync();
                    telemetry.NetworkOut = await GetNetworkOutAsync();
                    break;
                default:
                    // Collect all metrics
                    telemetry.CpuUsage = await GetCpuUsageAsync();
                    telemetry.MemoryUsage = await GetMemoryUsageAsync();
                    telemetry.DiskUsage = await GetDiskUsageAsync();
                    telemetry.NetworkIn = await GetNetworkInAsync();
                    telemetry.NetworkOut = await GetNetworkOutAsync();
                    break;
            }

            return telemetry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting on-demand telemetry");
            return new TelemetryDto
            {
                AgentId = _agentId,
                TenantCode = _tenantCode,
                Timestamp = DateTime.UtcNow,
                CustomMetrics = new Dictionary<string, object>
                {
                    { "Error", ex.Message },
                    { "MetricType", metricType }
                }
            };
        }
    }

    public async Task<TelemetryDto> CollectEventTelemetryAsync(string eventType, Dictionary<string, object> eventData)
    {
        try
        {
            var telemetry = new TelemetryDto
            {
                AgentId = _agentId,
                TenantCode = _tenantCode,
                Timestamp = DateTime.UtcNow,
                CustomMetrics = new Dictionary<string, object>
                {
                    { "EventType", eventType },
                    { "EventData", eventData }
                }
            };

            return telemetry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting event telemetry");
            return new TelemetryDto
            {
                AgentId = _agentId,
                TenantCode = _tenantCode,
                Timestamp = DateTime.UtcNow,
                CustomMetrics = new Dictionary<string, object>
                {
                    { "Error", ex.Message },
                    { "EventType", eventType }
                }
            };
        }
    }

    public async Task<TelemetryDto[]> CollectSpecificMetricsAsync(string[] metrics)
    {
        var results = new List<TelemetryDto>();
        
        foreach (var metric in metrics)
        {
            var telemetry = await CollectOnDemandTelemetryAsync(metric);
            results.Add(telemetry);
        }
        
        return results.ToArray();
    }

    public async Task<TelemetryDto> CollectTelemetryAsync(string[] metrics)
    {
        var telemetry = new TelemetryDto
        {
            AgentId = _agentId,
            TenantCode = _tenantCode,
            Timestamp = DateTime.UtcNow,
            CustomMetrics = new Dictionary<string, object>
            {
                { "RequestedMetrics", metrics }
            }
        };

        foreach (var metric in metrics)
        {
            switch (metric.ToLower())
            {
                case "cpu":
                    telemetry.CpuUsage = await GetCpuUsageAsync();
                    break;
                case "memory":
                    telemetry.MemoryUsage = await GetMemoryUsageAsync();
                    break;
                case "disk":
                    telemetry.DiskUsage = await GetDiskUsageAsync();
                    break;
                case "network":
                    telemetry.NetworkIn = await GetNetworkInAsync();
                    telemetry.NetworkOut = await GetNetworkOutAsync();
                    break;
                case "system":
                    telemetry.SystemInfo = await GetSystemInfoAsync();
                    break;
            }
        }

        return telemetry;
    }

    private async Task<double> GetCpuUsageAsync()
    {
        try
        {
            // Simple CPU usage calculation
            await Task.Delay(100);
            return Math.Round(Random.Shared.NextDouble() * 100, 2); // Placeholder
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get CPU usage");
            return 0;
        }
    }

    private async Task<double> GetMemoryUsageAsync()
    {
        try
        {
            await Task.Delay(50);
            var totalMemory = GC.GetTotalMemory(false);
            return Math.Round(totalMemory / (1024.0 * 1024.0), 2); // MB
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get memory usage");
            return 0;
        }
    }

    private async Task<double> GetDiskUsageAsync()
    {
        try
        {
            await Task.Delay(50);
            var drives = DriveInfo.GetDrives();
            if (drives.Length > 0)
            {
                var primaryDrive = drives.First();
                if (primaryDrive.IsReady)
                {
                    var usedSpace = primaryDrive.TotalSize - primaryDrive.AvailableFreeSpace;
                    return Math.Round((double)usedSpace / primaryDrive.TotalSize * 100, 2);
                }
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get disk usage");
            return 0;
        }
    }

    private async Task<double> GetNetworkInAsync()
    {
        try
        {
            await Task.Delay(50);
            return Math.Round(Random.Shared.NextDouble() * 1024, 2); // Placeholder
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get network in");
            return 0;
        }
    }

    private async Task<double> GetNetworkOutAsync()
    {
        try
        {
            await Task.Delay(50);
            return Math.Round(Random.Shared.NextDouble() * 1024, 2); // Placeholder
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get network out");
            return 0;
        }
    }

    private async Task<Signal9.Shared.DTOs.SystemInfoDto> GetSystemInfoAsync()
    {
        try
        {
            await Task.Delay(50);
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => new DriveInfoDto
                {
                    Name = d.Name,
                    DriveType = d.DriveType.ToString(),
                    TotalSize = d.TotalSize,
                    AvailableSpace = d.AvailableFreeSpace
                }).ToList();

            return new Signal9.Shared.DTOs.SystemInfoDto
            {
                OperatingSystem = Environment.OSVersion.ToString(),
                Architecture = Environment.OSVersion.Platform.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                TotalMemory = GC.GetTotalMemory(false),
                AvailableMemory = GC.GetTotalMemory(false), // Simplified
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
                Drives = drives
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get system info");
            return new Signal9.Shared.DTOs.SystemInfoDto();
        }
    }
}
