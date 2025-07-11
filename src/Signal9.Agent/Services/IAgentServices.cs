using Signal9.Shared.DTOs;

namespace Signal9.Agent.Services;

/// <summary>
/// Interface for collecting telemetry data from the system
/// </summary>
public interface ITelemetryCollector
{
    Task<TelemetryDto> CollectTelemetryAsync();
    Task<TelemetryDto[]> CollectSpecificMetricsAsync(string[] metrics);
    Task<TelemetryDto> CollectTelemetryAsync(string[] metrics);
}

/// <summary>
/// Interface for getting system information
/// </summary>
public interface ISystemInfoProvider
{
    Task<SystemInfoDto> GetSystemInfoAsync();
    Task<PerformanceMetricsDto> GetPerformanceMetricsAsync();
}

/// <summary>
/// Data transfer object for system information
/// </summary>
public class SystemInfoDto
{
    public string MachineName { get; set; } = Environment.MachineName;
    public string? Domain { get; set; }
    public string OperatingSystem { get; set; } = Environment.OSVersion.Platform.ToString();
    public string? OSVersion { get; set; } = Environment.OSVersion.VersionString;
    public string? Architecture { get; set; } = Environment.Is64BitOperatingSystem ? "x64" : "x86";
    public long TotalMemoryMB { get; set; }
    public int ProcessorCores { get; set; } = Environment.ProcessorCount;
    public string? ProcessorName { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? MacAddress { get; set; }
    public string? Version { get; set; } = "1.0.0";
}

/// <summary>
/// Data transfer object for performance metrics
/// </summary>
public class PerformanceMetricsDto
{
    public double CpuUsagePercent { get; set; }
    public long MemoryUsedMB { get; set; }
    public long MemoryAvailableMB { get; set; }
    public double DiskUsagePercent { get; set; }
    public long DiskFreeSpaceGB { get; set; }
    public double NetworkBytesInPerSec { get; set; }
    public double NetworkBytesOutPerSec { get; set; }
    public int ProcessCount { get; set; }
    public int ServiceCount { get; set; }
    public double SystemUptime { get; set; }
}
