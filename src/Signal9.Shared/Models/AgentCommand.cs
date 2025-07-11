using System.ComponentModel.DataAnnotations;

namespace Signal9.Shared.Models;

/// <summary>
/// Represents a command sent to an agent
/// </summary>
public class AgentCommand
{
    [Key]
    public Guid CommandId { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid AgentId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string CommandType { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Parameters { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExecutedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public CommandStatus Status { get; set; } = CommandStatus.Pending;
    
    [MaxLength(2000)]
    public string? Result { get; set; }
    
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }
    
    public int? ExitCode { get; set; }
    
    [MaxLength(255)]
    public string? CreatedBy { get; set; }
    
    public int Priority { get; set; } = 0;
    
    public DateTime? ExpiresAt { get; set; }
    
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum CommandStatus
{
    Pending,
    Sent,
    Executing,
    Completed,
    Failed,
    Cancelled,
    Expired
}
