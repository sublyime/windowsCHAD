using System.ComponentModel.DataAnnotations;

namespace ChemicalDispersionModeling.Core.Models;

/// <summary>
/// Represents a chemical release event for dispersion modeling
/// </summary>
public class Release
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Latitude of release point in decimal degrees
    /// </summary>
    public double Latitude { get; set; }
    
    /// <summary>
    /// Longitude of release point in decimal degrees
    /// </summary>
    public double Longitude { get; set; }
    
    /// <summary>
    /// Elevation of release point in meters above sea level
    /// </summary>
    public double Elevation { get; set; }
    
    /// <summary>
    /// Height of release above ground in meters
    /// </summary>
    public double ReleaseHeight { get; set; }
    
    /// <summary>
    /// Start time of the release
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// End time of the release (null for continuous)
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Release rate in kg/s for continuous releases
    /// </summary>
    public double? ReleaseRate { get; set; }
    
    /// <summary>
    /// Total mass released in kg for instantaneous releases
    /// </summary>
    public double? TotalMass { get; set; }
    
    /// <summary>
    /// Volume released in m³ for liquid releases
    /// </summary>
    public double? Volume { get; set; }
    
    /// <summary>
    /// Type of release: Instantaneous, Continuous, Variable
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ReleaseType { get; set; } = "Instantaneous";
    
    /// <summary>
    /// Release scenario: Gas, Liquid Pool, Fire, Explosion, etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Scenario { get; set; } = "Gas";
    
    /// <summary>
    /// Initial temperature of released material in Celsius
    /// </summary>
    public double? InitialTemperature { get; set; }
    
    /// <summary>
    /// Initial pressure of released material in Pa
    /// </summary>
    public double? InitialPressure { get; set; }
    
    /// <summary>
    /// Jet/leak diameter in meters for pressurized releases
    /// </summary>
    public double? DiameterOrArea { get; set; }
    
    /// <summary>
    /// Wind speed used for modeling in m/s
    /// </summary>
    public double ModelingWindSpeed { get; set; }
    
    /// <summary>
    /// Wind direction used for modeling in degrees
    /// </summary>
    public double ModelingWindDirection { get; set; }
    
    /// <summary>
    /// Atmospheric stability class used for modeling
    /// </summary>
    [MaxLength(1)]
    public string ModelingStabilityClass { get; set; } = "D";
    
    /// <summary>
    /// Temperature used for modeling in Celsius
    /// </summary>
    public double ModelingTemperature { get; set; }
    
    /// <summary>
    /// Humidity used for modeling (0-100%)
    /// </summary>
    public double ModelingHumidity { get; set; }
    
    /// <summary>
    /// Status of the release: Draft, Active, Completed, Archived
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Draft";
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(100)]
    public string? CreatedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public int ChemicalId { get; set; }
    public int? WeatherDataId { get; set; }
    
    // Navigation properties
    public virtual Chemical Chemical { get; set; } = null!;
    public virtual WeatherData? WeatherData { get; set; }
    public virtual ICollection<DispersionResult> DispersionResults { get; set; } = new List<DispersionResult>();
    public virtual ICollection<Receptor> Receptors { get; set; } = new List<Receptor>();
}