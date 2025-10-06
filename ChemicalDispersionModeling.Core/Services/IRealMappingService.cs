using ChemicalDispersionModeling.Core.Models;

namespace ChemicalDispersionModeling.Core.Services;

/// <summary>
/// Interface for real mapping services with actual geographic data
/// </summary>
public interface IRealMappingService
{
    /// <summary>
    /// Initialize mapping with specified provider
    /// </summary>
    Task<bool> InitializeAsync(MapProvider provider, string apiKey = "");
    
    /// <summary>
    /// Set map center and zoom level
    /// </summary>
    Task SetMapViewAsync(double latitude, double longitude, int zoomLevel);
    
    /// <summary>
    /// Add dispersion plume overlay to map
    /// </summary>
    Task AddDispersionPlumeAsync(DispersionResult result, DispersionVisualizationOptions options);
    
    /// <summary>
    /// Clear all dispersion overlays
    /// </summary>
    Task ClearDispersionOverlaysAsync();
    
    /// <summary>
    /// Add weather station markers
    /// </summary>
    Task AddWeatherStationAsync(WeatherStation station);
    
    /// <summary>
    /// Add gas sensor markers
    /// </summary>
    Task AddGasSensorAsync(GasSensor sensor);
    
    /// <summary>
    /// Add release source marker
    /// </summary>
    Task AddReleaseSourceAsync(Release release);
    
    /// <summary>
    /// Add receptor points
    /// </summary>
    Task AddReceptorAsync(Receptor receptor);
    
    /// <summary>
    /// Load terrain/elevation data for area
    /// </summary>
    Task<TerrainData> LoadTerrainDataAsync(double minLat, double minLon, double maxLat, double maxLon);
    
    /// <summary>
    /// Load building footprint data
    /// </summary>
    Task LoadBuildingDataAsync(double minLat, double minLon, double maxLat, double maxLon);
    
    /// <summary>
    /// Convert screen coordinates to geographic coordinates
    /// </summary>
    Task<(double latitude, double longitude)> ScreenToGeographicAsync(double x, double y);
    
    /// <summary>
    /// Convert geographic coordinates to screen coordinates
    /// </summary>
    Task<(double x, double y)> GeographicToScreenAsync(double latitude, double longitude);
    
    /// <summary>
    /// Event fired when map is clicked
    /// </summary>
    event EventHandler<MapClickEventArgs> MapClicked;
    
    /// <summary>
    /// Event fired when map view changes
    /// </summary>
    event EventHandler<MapViewChangedEventArgs> MapViewChanged;
}

/// <summary>
/// Map providers for real mapping
/// </summary>
public enum MapProvider
{
    OpenStreetMap,
    GoogleMaps,
    BingMaps,
    ArcGISOnline,
    MapBox,
    ESRI
}

/// <summary>
/// Options for visualizing dispersion results
/// </summary>
public class DispersionVisualizationOptions
{
    public List<double> ConcentrationLevels { get; set; } = new();
    public List<string> ContourColors { get; set; } = new();
    public double Opacity { get; set; } = 0.6;
    public bool ShowConcentrationLabels { get; set; } = true;
    public bool ShowFootprint { get; set; } = true;
    public bool ShowCenterline { get; set; } = true;
    public bool Animate { get; set; } = false;
    public int AnimationDurationMs { get; set; } = 1000;
}

/// <summary>
/// Weather station data structure
/// </summary>
public class WeatherStation
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Elevation { get; set; }
    public WeatherData? CurrentWeather { get; set; }
    public DataSourceConfiguration DataSource { get; set; } = new();
    public bool IsOnline { get; set; }
}

/// <summary>
/// Gas sensor data structure
/// </summary>
public class GasSensor
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Elevation { get; set; }
    public List<string> DetectedChemicals { get; set; } = new();
    public GasSensorReading? LatestReading { get; set; }
    public DataSourceConfiguration DataSource { get; set; } = new();
    public bool IsOnline { get; set; }
    public double AlertThreshold { get; set; }
    public string AlertUnit { get; set; } = "ppm";
}

/// <summary>
/// Map click event arguments
/// </summary>
public class MapClickEventArgs : EventArgs
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double ScreenX { get; set; }
    public double ScreenY { get; set; }
    public bool IsRightClick { get; set; }
}

/// <summary>
/// Map view changed event arguments
/// </summary>
public class MapViewChangedEventArgs : EventArgs
{
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public int ZoomLevel { get; set; }
    public double MinLatitude { get; set; }
    public double MinLongitude { get; set; }
    public double MaxLatitude { get; set; }
    public double MaxLongitude { get; set; }
}