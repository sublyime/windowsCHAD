using System.ComponentModel.DataAnnotations;

namespace ChemicalDispersionModeling.Core.Models;

/// <summary>
/// Represents a chemical substance with its properties for dispersion modeling
/// </summary>
public class Chemical
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? CasNumber { get; set; }
    
    [MaxLength(20)]
    public string PhysicalState { get; set; } = "Gas"; // Gas, Liquid, Solid
    
    /// <summary>
    /// Molecular weight in g/mol
    /// </summary>
    public double MolecularWeight { get; set; }
    
    /// <summary>
    /// Vapor pressure in Pa at 25°C
    /// </summary>
    public double? VaporPressure { get; set; }
    
    /// <summary>
    /// Boiling point in Celsius
    /// </summary>
    public double? BoilingPoint { get; set; }
    
    /// <summary>
    /// Melting point in Celsius
    /// </summary>
    public double? MeltingPoint { get; set; }
    
    /// <summary>
    /// Density in kg/m³
    /// </summary>
    public double Density { get; set; }
    
    /// <summary>
    /// Henry's law constant (atm⋅m³/mol)
    /// </summary>
    public double? HenryConstant { get; set; }
    
    /// <summary>
    /// Diffusion coefficient in air (m²/s)
    /// </summary>
    public double? DiffusionCoefficient { get; set; }
    
    /// <summary>
    /// Toxicity threshold (ppm or mg/m³)
    /// </summary>
    public double? ToxicityThreshold { get; set; }
    
    [MaxLength(20)]
    public string? ToxicityUnit { get; set; }
    
    /// <summary>
    /// Flammability properties
    /// </summary>
    public bool IsFlammable { get; set; }
    
    /// <summary>
    /// Lower explosive limit (%)
    /// </summary>
    public double? LowerExplosiveLimit { get; set; }
    
    /// <summary>
    /// Upper explosive limit (%)
    /// </summary>
    public double? UpperExplosiveLimit { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Release> Releases { get; set; } = new List<Release>();
}