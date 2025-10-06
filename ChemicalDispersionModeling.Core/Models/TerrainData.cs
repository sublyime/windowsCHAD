using System.ComponentModel.DataAnnotations;

namespace ChemicalDispersionModeling.Core.Models;

/// <summary>
/// Represents topographical and building data for dispersion modeling
/// </summary>
public class TerrainData
{
    public int Id { get; set; }
    
    /// <summary>
    /// Latitude of the terrain point
    /// </summary>
    public double Latitude { get; set; }
    
    /// <summary>
    /// Longitude of the terrain point
    /// </summary>
    public double Longitude { get; set; }
    
    /// <summary>
    /// Ground elevation in meters above sea level
    /// </summary>
    public double Elevation { get; set; }
    
    /// <summary>
    /// Land use type: Urban, Suburban, Rural, Industrial, Water, Forest, etc.
    /// </summary>
    [MaxLength(50)]
    public string LandUseType { get; set; } = "Urban";
    
    /// <summary>
    /// Surface roughness length in meters
    /// </summary>
    public double SurfaceRoughness { get; set; }
    
    /// <summary>
    /// Building height in meters (0 for no building)
    /// </summary>
    public double BuildingHeight { get; set; } = 0;
    
    /// <summary>
    /// Building width in meters
    /// </summary>
    public double? BuildingWidth { get; set; }
    
    /// <summary>
    /// Building length in meters
    /// </summary>
    public double? BuildingLength { get; set; }
    
    /// <summary>
    /// Building orientation in degrees from North
    /// </summary>
    public double? BuildingOrientation { get; set; }
    
    /// <summary>
    /// Whether this represents a building
    /// </summary>
    public bool IsBuilding { get; set; }
    
    /// <summary>
    /// Building type: Residential, Commercial, Industrial, etc.
    /// </summary>
    [MaxLength(50)]
    public string? BuildingType { get; set; }
    
    /// <summary>
    /// Data source: Survey, LIDAR, Satellite, Manual, etc.
    /// </summary>
    [MaxLength(50)]
    public string DataSource { get; set; } = "Manual";
    
    /// <summary>
    /// Resolution of the data in meters
    /// </summary>
    public double? DataResolution { get; set; }
    
    /// <summary>
    /// Date when the terrain data was collected
    /// </summary>
    public DateTime? DataDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}