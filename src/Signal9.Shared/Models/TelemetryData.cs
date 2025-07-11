using System.ComponentModel.DataAnnotations;

namespace Signal9.Shared.Models;

/// <summary>
/// Represents telemetry data collected from an agent
/// </summary>
public class TelemetryData
{
    [Key]
    public Guid TelemetryId { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid AgentId { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public TelemetryType Type { get; set; }
    
    // Performance Metrics
    public double? CpuUsagePercent { get; set; }
    
    public long? MemoryUsedMB { get; set; }
    
    public long? MemoryAvailableMB { get; set; }
    
    public double? DiskUsagePercent { get; set; }
    
    public long? DiskFreeSpaceGB { get; set; }
    
    public double? NetworkBytesInPerSec { get; set; }
    
    public double? NetworkBytesOutPerSec { get; set; }
    
    // System Health
    public int? ProcessCount { get; set; }
    
    public int? ServiceCount { get; set; }
    
    public double? SystemUptime { get; set; }
    
    // Custom Metrics
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
    
    // Event Data
    [MaxLength(100)]
    public string? EventLevel { get; set; }
    
    [MaxLength(255)]
    public string? EventSource { get; set; }
    
    [MaxLength(1000)]
    public string? EventMessage { get; set; }
    
    public Dictionary<string, object> EventData { get; set; } = new();
}

public enum TelemetryType
{
    Performance,
    SystemHealth,
    Event,
    Security,
    Application,
    Network,
    Storage,
    Custom
}
