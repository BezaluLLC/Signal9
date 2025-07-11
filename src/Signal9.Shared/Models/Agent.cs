using System.ComponentModel.DataAnnotations;

namespace Signal9.Shared.Models;

/// <summary>
/// Represents an RMM agent installed on a client machine
/// </summary>
public class Agent
{
    [Key]
    public Guid AgentId { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(255)]
    public string MachineName { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? Domain { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string OperatingSystem { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? OSVersion { get; set; }
    
    [MaxLength(100)]
    public string? Architecture { get; set; }
    
    public long TotalMemoryMB { get; set; }
    
    public int ProcessorCores { get; set; }
    
    [MaxLength(500)]
    public string? ProcessorName { get; set; }
    
    [Required]
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [MaxLength(17)]
    public string? MacAddress { get; set; }
    
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
    
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    
    public AgentStatus Status { get; set; } = AgentStatus.Online;
    
    [MaxLength(50)]
    public string? Version { get; set; }
    
    public Guid? TenantId { get; set; }
    
    [MaxLength(255)]
    public string? GroupName { get; set; }
    
    public Dictionary<string, object> Tags { get; set; } = new();
    
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}

public enum AgentStatus
{
    Online,
    Offline,
    Maintenance,
    Error,
    Unregistered
}
