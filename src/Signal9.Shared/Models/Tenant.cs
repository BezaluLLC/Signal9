using System.ComponentModel.DataAnnotations;

namespace Signal9.Shared.Models;

/// <summary>
/// Represents a tenant/organization in the multi-tenant RMM system
/// </summary>
public class Tenant
{
    [Key]
    public Guid TenantId { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Code { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(255)]
    public string? ContactEmail { get; set; }
    
    [MaxLength(50)]
    public string? ContactPhone { get; set; }
    
    public TenantPlan Plan { get; set; } = TenantPlan.Basic;
    
    public int MaxAgents { get; set; } = 10;
    
    public Dictionary<string, object> Settings { get; set; } = new();
    
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum TenantPlan
{
    Basic,
    Professional,
    Enterprise,
    Custom
}
