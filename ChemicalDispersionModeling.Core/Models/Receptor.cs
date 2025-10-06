using System.ComponentModel.DataAnnotations;

namespace ChemicalDispersionModeling.Core.Models;

/// <summary>
/// Represents a receptor point for impact assessment
/// </summary>
public class Receptor
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Latitude in decimal degrees
    /// </summary>
    public double Latitude { get; set; }
    
    /// <summary>
    /// Longitude in decimal degrees
    /// </summary>
    public double Longitude { get; set; }
    
    /// <summary>
    /// Elevation in meters above sea level
    /// </summary>
    public double Elevation { get; set; }
    
    /// <summary>
    /// Height above ground in meters (breathing height)
    /// </summary>
    public double Height { get; set; } = 1.5;
    
    /// <summary>
    /// Type of receptor: Residential, Commercial, Industrial, Environmental, etc.
    /// </summary>
    [MaxLength(50)]
    public string ReceptorType { get; set; } = "Residential";
    
    /// <summary>
    /// Population at this receptor point
    /// </summary>
    public int? Population { get; set; }
    
    /// <summary>
    /// Whether this receptor is currently active for modeling
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign key
    public int ReleaseId { get; set; }
    
    // Navigation properties
    public virtual Release Release { get; set; } = null!;
    public virtual ICollection<DispersionResult> DispersionResults { get; set; } = new List<DispersionResult>();
}