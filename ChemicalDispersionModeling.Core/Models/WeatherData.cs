using System.ComponentModel.DataAnnotations;

namespace ChemicalDispersionModeling.Core.Models;

/// <summary>
/// Represents weather data at a specific time and location
/// </summary>
public class WeatherData
{
    public int Id { get; set; }
    
    /// <summary>
    /// Weather station identifier or coordinates-based ID
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string StationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Latitude in decimal degrees
    /// </summary>
    public double Latitude { get; set; }
    
    /// <summary>
    /// Longitude in decimal degrees
    /// </summary>
    public double Longitude { get; set; }
    
    /// <summary>
    /// Timestamp of the weather observation
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Temperature in Celsius
    /// </summary>
    public double Temperature { get; set; }
    
    /// <summary>
    /// Relative humidity (0-100%)
    /// </summary>
    public double Humidity { get; set; }
    
    /// <summary>
    /// Atmospheric pressure in hPa (millibars)
    /// </summary>
    public double Pressure { get; set; }
    
    /// <summary>
    /// Wind speed in m/s
    /// </summary>
    public double WindSpeed { get; set; }
    
    /// <summary>
    /// Wind direction in degrees (0-360, where 0/360 is North)
    /// </summary>
    public double WindDirection { get; set; }
    
    /// <summary>
    /// Wind gust speed in m/s
    /// </summary>
    public double? WindGust { get; set; }
    
    /// <summary>
    /// Visibility in meters
    /// </summary>
    public double? Visibility { get; set; }
    
    /// <summary>
    /// Cloud cover percentage (0-100%)
    /// </summary>
    public double? CloudCover { get; set; }
    
    /// <summary>
    /// Precipitation rate in mm/h
    /// </summary>
    public double? PrecipitationRate { get; set; }
    
    /// <summary>
    /// Solar radiation in W/m²
    /// </summary>
    public double? SolarRadiation { get; set; }
    
    /// <summary>
    /// Atmospheric stability class (A-F, Pasquill-Gifford)
    /// </summary>
    [MaxLength(1)]
    public string? StabilityClass { get; set; }
    
    /// <summary>
    /// Mixing layer height in meters
    /// </summary>
    public double? MixingLayerHeight { get; set; }
    
    /// <summary>
    /// Source of weather data (e.g., "NWS", "OpenMeteo", "Local Station")
    /// </summary>
    [MaxLength(50)]
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// Data source identifier for live data services
    /// </summary>
    [MaxLength(100)]
    public string? DataSource { get; set; }
    
    /// <summary>
    /// Quality flag for data validation (Good, Fair, Poor, Invalid)
    /// </summary>
    [MaxLength(20)]
    public string? QualityFlag { get; set; }
    
    /// <summary>
    /// Raw JSON data from the weather service
    /// </summary>
    public string? RawData { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Release> Releases { get; set; } = new List<Release>();
}