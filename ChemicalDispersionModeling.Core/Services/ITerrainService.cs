using ChemicalDispersionModeling.Core.Models;

namespace ChemicalDispersionModeling.Core.Services;

/// <summary>
/// Service for terrain and building data management
/// </summary>
public interface ITerrainService
{
    /// <summary>
    /// Get terrain data for a specific area
    /// </summary>
    Task<IEnumerable<TerrainData>> GetTerrainDataAsync(double minLat, double minLon, double maxLat, double maxLon);
    
    /// <summary>
    /// Get elevation at a specific point
    /// </summary>
    Task<double> GetElevationAsync(double latitude, double longitude);
    
    /// <summary>
    /// Get building data around a specific point
    /// </summary>
    Task<IEnumerable<TerrainData>> GetBuildingsAsync(double latitude, double longitude, double radiusMeters);
    
    /// <summary>
    /// Import terrain data from GIS files
    /// </summary>
    Task ImportTerrainDataAsync(string filePath, string fileType);
    
    /// <summary>
    /// Calculate surface roughness for a given area
    /// </summary>
    Task<double> CalculateSurfaceRoughnessAsync(double latitude, double longitude, double radiusMeters);
    
    /// <summary>
    /// Check if there are buildings that might affect dispersion
    /// </summary>
    Task<bool> HasSignificantBuildingsAsync(double latitude, double longitude, double radiusMeters, double minimumHeight = 10.0);
}