using System.ComponentModel.DataAnnotations;

namespace ChemicalDispersionModeling.Core.Models;

/// <summary>
/// Represents the results of dispersion modeling at specific times and locations
/// </summary>
public class DispersionResult
{
    public int Id { get; set; }
    
    /// <summary>
    /// Time of the calculation
    /// </summary>
    public DateTime CalculationTime { get; set; }
    
    /// <summary>
    /// Latitude of the result point
    /// </summary>
    public double Latitude { get; set; }
    
    /// <summary>
    /// Longitude of the result point
    /// </summary>
    public double Longitude { get; set; }
    
    /// <summary>
    /// Height above ground in meters
    /// </summary>
    public double Height { get; set; }
    
    /// <summary>
    /// Concentration in mg/m³ or ppm
    /// </summary>
    public double Concentration { get; set; }
    
    /// <summary>
    /// Unit of concentration measurement
    /// </summary>
    [MaxLength(10)]
    public string ConcentrationUnit { get; set; } = "mg/m³";
    
    /// <summary>
    /// Dosage (concentration × time) in mg⋅min/m³
    /// </summary>
    public double? Dosage { get; set; }
    
    /// <summary>
    /// Distance from release point in meters
    /// </summary>
    public double DistanceFromSource { get; set; }
    
    /// <summary>
    /// Direction from release point in degrees
    /// </summary>
    public double DirectionFromSource { get; set; }
    
    /// <summary>
    /// Plume width at this point in meters
    /// </summary>
    public double? PlumeWidth { get; set; }
    
    /// <summary>
    /// Plume height at this point in meters
    /// </summary>
    public double? PlumeHeight { get; set; }
    
    /// <summary>
    /// Wind speed at calculation time in m/s
    /// </summary>
    public double WindSpeed { get; set; }
    
    /// <summary>
    /// Wind direction at calculation time in degrees
    /// </summary>
    public double WindDirection { get; set; }
    
    /// <summary>
    /// Atmospheric stability class at calculation time
    /// </summary>
    [MaxLength(1)]
    public string StabilityClass { get; set; } = "D";
    
    /// <summary>
    /// Temperature at calculation time in Celsius
    /// </summary>
    public double Temperature { get; set; }
    
    /// <summary>
    /// Whether this concentration exceeds toxicity thresholds
    /// </summary>
    public bool ExceedsToxicityThreshold { get; set; }
    
    /// <summary>
    /// Risk level: Low, Medium, High, Critical
    /// </summary>
    [MaxLength(20)]
    public string RiskLevel { get; set; } = "Low";
    
    /// <summary>
    /// Model used for calculation
    /// </summary>
    [MaxLength(50)]
    public string ModelUsed { get; set; } = "Gaussian Plume";
    
    /// <summary>
    /// Additional calculation parameters in JSON format
    /// </summary>
    public string? CalculationParameters { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public int ReleaseId { get; set; }
    public int? ReceptorId { get; set; }
    
    // Navigation properties
    public virtual Release Release { get; set; } = null!;
    public virtual Receptor? Receptor { get; set; }
}