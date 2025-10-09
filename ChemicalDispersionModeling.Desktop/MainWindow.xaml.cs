using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using IOPath = System.IO.Path;
using ChemicalDispersionModeling.Desktop.ViewModels;
using ChemicalDispersionModeling.Desktop.Services;
using ChemicalDispersionModeling.Core.Services;
using ChemicalDispersionModeling.Core.Services.Implementation;
using ChemicalDispersionModeling.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;

namespace ChemicalDispersionModeling.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;
    private ILiveWeatherService? _liveWeatherService;
    private IGasSensorService? _gasSensorService;
    private IRealMappingService? _realMappingService;
    private IDispersionModelingService? _dispersionService;
    private ILogger<MainWindow>? _logger;
    private WebView2? _mapWebView;

    // Parameterless constructor for XAML
    public MainWindow()
    {
        InitializeComponent();
    }

    // Constructor for dependency injection
    public MainWindow(
        ILiveWeatherService liveWeatherService,
        IGasSensorService gasSensorService,
        IRealMappingService realMappingService,
        IDispersionModelingService dispersionService,
        ILogger<MainWindow> logger) : this()
    {
        Console.WriteLine("=== MainWindow Constructor Called ===");
        _liveWeatherService = liveWeatherService;
        _gasSensorService = gasSensorService;
        _realMappingService = realMappingService;
        _dispersionService = dispersionService;
        _logger = logger;

        try
        {
            _logger?.LogInformation("MainWindow constructor started");
            
            this.Loaded += MainWindow_Loaded;
            _logger?.LogInformation("Loaded event handler attached");
            
            // Subscribe to live data events with error handling
            try
            {
                _liveWeatherService.WeatherDataReceived += OnWeatherDataReceived;
                _logger?.LogInformation("Weather service event subscribed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to subscribe to weather service events");
            }
            
            try
            {
                _gasSensorService.GasSensorDataReceived += OnGasSensorDataReceived;
                _logger?.LogInformation("Gas sensor service event subscribed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to subscribe to gas sensor service events");
            }
            
            _logger?.LogInformation("MainWindow constructor completed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in MainWindow constructor");
            throw;
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger?.LogInformation("=== MAIN WINDOW LOADED EVENT ===");
            
            // Use the DataContext that was already set by App.xaml.cs
            _viewModel = this.DataContext as MainViewModel;
            if (_viewModel != null)
            {
                _logger?.LogInformation("ViewModel retrieved from DataContext");
                
                // Subscribe to dispersion model completed event
                _viewModel.DispersionModelCompleted += OnDispersionModelCompleted;
                
                // Set default coordinates
                _viewModel.ReleaseLatitude = 40.7831;
                _viewModel.ReleaseLongitude = -73.9712;
                _logger?.LogInformation($"Default coordinates set: {_viewModel.ReleaseLatitude}, {_viewModel.ReleaseLongitude}");
            }
            else
            {
                _logger?.LogError("Failed to get ViewModel from DataContext");
                return;
            }
            
            // Find the WebView2 control
            _mapWebView = this.FindName("MapWebView") as WebView2;
            _logger?.LogInformation($"WebView2 control found: {_mapWebView != null}");
            
            if (_mapWebView != null)
            {
                try
                {
                    _logger?.LogInformation("Ensuring WebView2 core is ready...");
                    await _mapWebView.EnsureCoreWebView2Async();
                    _logger?.LogInformation("WebView2 core initialized successfully");
                    
                    // Subscribe to web message received
                    _mapWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                    _logger?.LogInformation("WebMessageReceived event subscribed");
                    
                    // Load the map HTML
                    var htmlPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "map.html");
                    _logger?.LogInformation($"Loading map from: {htmlPath}");
                    
                    if (File.Exists(htmlPath))
                    {
                        _mapWebView.CoreWebView2.Navigate($"file:///{htmlPath.Replace('\\', '/')}");
                        _logger?.LogInformation("Map HTML loaded successfully");
                    }
                    else
                    {
                        _logger?.LogError($"Map HTML file not found at: {htmlPath}");
                        
                        // Create a simple default map if file doesn't exist
                        var defaultHtml = CreateDefaultMapHtml();
                        _mapWebView.NavigateToString(defaultHtml);
                        _logger?.LogInformation("Default map HTML created and loaded");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error initializing WebView2");
                }
            }
            
            // Set up mapping service events
            if (_realMappingService is RealMappingService realMapping && _mapWebView != null)
            {
                try
                {
                    realMapping.SetWebView(_mapWebView);
                    realMapping.MapClicked += OnMapClicked;
                    realMapping.MapViewChanged += OnMapViewChanged;
                    _logger?.LogInformation("Mapping service events configured");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error setting up mapping service events");
                }
            }
            
            // Start live weather monitoring
            try
            {
                if (_liveWeatherService != null)
                {
                    await _liveWeatherService.StartContinuousMonitoringAsync(_viewModel.ReleaseLatitude, _viewModel.ReleaseLongitude, DataSourceType.OpenMeteo);
                    _logger?.LogInformation("Live weather monitoring started");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error starting live weather monitoring");
            }
            
            // Start gas sensor monitoring
            try
            {
                if (_gasSensorService != null)
                {
                    var config = new DataSourceConfiguration 
                    { 
                        SourceType = DataSourceType.File,
                        PollingIntervalSeconds = 30
                    };
                    await _gasSensorService.StartContinuousMonitoringAsync(config);
                    _logger?.LogInformation("Gas sensor monitoring started");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error starting gas sensor monitoring");
            }
            
            _logger?.LogInformation("MainWindow loaded and all services started successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in MainWindow_Loaded");
            
            // Show error to user
            MessageBox.Show($"Error loading application: {ex.Message}", "Startup Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CoreWebView2_WebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = e.TryGetWebMessageAsString();
            _logger?.LogInformation($"WebMessage received: {message}");
            
            // Parse the JSON message for map clicks
            if (!string.IsNullOrEmpty(message) && message.Contains("mapClick"))
            {
                dynamic? data = System.Text.Json.JsonSerializer.Deserialize<dynamic>(message);
                if (data != null)
                {
                    _logger?.LogInformation($"Parsed map click data: {data}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing web message");
        }
    }

    private void OnWeatherDataReceived(object? sender, WeatherData weatherData)
    {
        try
        {
            _logger?.LogInformation($"Live weather data received: Temp={weatherData.Temperature}°C, Wind={weatherData.WindSpeed} m/s");
            
            Dispatcher.Invoke(() =>
            {
                if (_viewModel != null)
                {
                    _viewModel.CurrentWeather = weatherData;
                    _logger?.LogInformation("Weather data updated in ViewModel");
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing weather data");
        }
    }

    private void OnGasSensorDataReceived(object? sender, IEnumerable<GasSensorReading> readings)
    {
        try
        {
            _logger?.LogInformation($"Gas sensor data received: {readings.Count()} readings");
            
            Dispatcher.Invoke(() =>
            {
                if (_viewModel != null)
                {
                    // Process the readings - could display them, trigger alerts, etc.
                    var latestReading = readings.LastOrDefault();
                    if (latestReading != null)
                    {
                        _logger?.LogInformation($"Latest reading: {latestReading.Concentration} {latestReading.ConcentrationUnit} at {latestReading.SensorLocation}");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing gas sensor data");
        }
    }

    private async void OnMapClicked(object? sender, MapClickEventArgs e)
    {
        try
        {
            _logger?.LogInformation($"=== MAP CLICK EVENT RECEIVED ====");
            _logger?.LogInformation($"Coordinates: Lat={e.Latitude:F6}, Lng={e.Longitude:F6}");
            _logger?.LogInformation($"Screen coordinates: X={e.ScreenX}, Y={e.ScreenY}");
            _logger?.LogInformation($"Sender: {sender?.GetType().Name}");
            _logger?.LogInformation($"ViewModel available: {_viewModel != null}");
            
            // Update the ViewModel with the new coordinates
            Dispatcher.Invoke(() =>
            {
                if (_viewModel != null)
                {
                    _logger?.LogInformation($"BEFORE UPDATE - Current location: Lat={_viewModel.ReleaseLatitude:F6}, Lng={_viewModel.ReleaseLongitude:F6}");
                    
                    // Update coordinates
                    _viewModel.ReleaseLatitude = e.Latitude;
                    _viewModel.ReleaseLongitude = e.Longitude;
                    
                    _logger?.LogInformation($"AFTER UPDATE - New location: Lat={_viewModel.ReleaseLatitude:F6}, Lng={_viewModel.ReleaseLongitude:F6}");
                    
                    // Update status messages
                    _viewModel.StatusMessage = $"Setting release location to {e.Latitude:F6}, {e.Longitude:F6}...";
                    _viewModel.DebugStatus = $"Debug: Map clicked at {e.Latitude:F6}, {e.Longitude:F6}";
                    
                    _logger?.LogInformation($"Status message set: {_viewModel.StatusMessage}");
                    _logger?.LogInformation("UI binding should update automatically via ObservableProperty");
                    
                    // Force UI refresh by updating multiple times
                    _viewModel.StatusMessage = $"Release location updated: {e.Latitude:F6}, {e.Longitude:F6}";
                }
                else
                {
                    _logger?.LogError("ViewModel is null - cannot update coordinates!");
                }
            });

            // Clear any existing release markers first - JavaScript already handles this
            _logger?.LogInformation($"=== MARKERS HANDLED BY JAVASCRIPT ====");
            _logger?.LogInformation($"JavaScript addReleaseMarker automatically clears previous markers");
            
            // Fetch weather data for the new location
            _logger?.LogInformation($"=== STARTING WEATHER UPDATE ====");
            if (_viewModel != null)
            {
                _logger?.LogInformation($"Calling UpdateWeatherForLocationAsync with Lat={e.Latitude:F6}, Lng={e.Longitude:F6}");
                await _viewModel.UpdateWeatherForLocationAsync(e.Latitude, e.Longitude);
                _logger?.LogInformation($"Weather update completed");
            }
            else
            {
                _logger?.LogError("ViewModel is null - cannot update weather!");
            }
            
            // Add a release source marker at the clicked location
            _logger?.LogInformation($"=== ADDING NEW RELEASE MARKER ====");
            _logger?.LogInformation($"Release marker already added by JavaScript - skipping AddReleaseSourceAsync");
            
            // Just update the view model data without adding another marker
            if (_viewModel != null)
            {
                var release = new Release
                {
                    Name = "Release Point",
                    Latitude = e.Latitude,
                    Longitude = e.Longitude,
                    ReleaseHeight = _viewModel.ReleaseHeight,
                    Chemical = _viewModel.SelectedChemical ?? new Chemical 
                    { 
                        Name = _viewModel.ChemicalName,
                        MolecularWeight = _viewModel.MolecularWeight,
                        Density = _viewModel.Density
                    },
                    ReleaseType = _viewModel.ReleaseType,
                    ReleaseRate = _viewModel.ReleaseRate,
                    TotalMass = _viewModel.TotalMass,
                    StartTime = DateTime.Now,
                    Scenario = _viewModel.ReleaseScenario
                };
                
                _logger?.LogInformation($"Release data updated for coordinates {release.Latitude:F6}, {release.Longitude:F6}");
                
                Dispatcher.Invoke(() =>
                {
                    if (_viewModel != null)
                    {
                        var weather = _viewModel.CurrentWeather;
                        var weatherInfo = weather != null 
                            ? $" | Weather: {weather.Temperature:F1}°C, Wind {weather.WindSpeed:F1} m/s"
                            : " | Loading weather...";
                        _viewModel.StatusMessage = $"Release location set: {e.Latitude:F6}, {e.Longitude:F6}{weatherInfo}";
                        _logger?.LogInformation($"Final status message: {_viewModel.StatusMessage}");
                        _logger?.LogInformation($"Weather data available: {weather != null}");
                    }
                });
            }
            
            _logger?.LogInformation($"Map clicked: {e.Latitude:F6}, {e.Longitude:F6}, weather updated, marker placed");
            
            // Note: Dispersion modeling will be triggered by the "Run Model" button, not automatically on map click
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing map click");
            Dispatcher.Invoke(() =>
            {
                if (_viewModel != null)
                {
                    _viewModel.StatusMessage = $"Error updating location: {ex.Message}";
                }
            });
        }
    }

    private async void OnDispersionModelCompleted(object? sender, DispersionModelCompletedEventArgs e)
    {
        try
        {
            _logger?.LogInformation("=== DISPERSION MODEL COMPLETED EVENT ===");
            _logger?.LogInformation($"Results count: {e.Results.Count()}");
            
            if (_realMappingService != null && e.Results.Any())
            {
                // Clear any existing plumes first
                await _realMappingService.ClearDispersionOverlaysAsync();
                _logger?.LogInformation("Cleared existing dispersion overlays");

                var dispersionVisualization = new DispersionVisualizationOptions
                {
                    ConcentrationLevels = new List<double> { 0.1, 1.0, 10.0, 100.0 }, // mg/m³
                    ContourColors = new List<string> { "#00FF00", "#FFFF00", "#FFA500", "#FF0000" },
                    Opacity = 0.7,
                    ShowConcentrationLabels = true,
                    ShowFootprint = true,
                    ShowCenterline = true
                };

                await _realMappingService.AddDispersionGridAsync(e.Results, dispersionVisualization);
                _logger?.LogInformation("Dispersion plume visualization added to map from Run Model button");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling dispersion model completed event");
        }
    }

    private async Task RunDispersionModelingAsync(double latitude, double longitude)
    {
        try
        {
            _logger?.LogInformation($"=== STARTING DISPERSION MODELING ===");
            
            if (_dispersionService == null || _viewModel == null)
            {
                _logger?.LogWarning("Dispersion service or view model is null - cannot run modeling");
                return;
            }
            
            var weather = _viewModel.CurrentWeather;
            if (weather == null)
            {
                _logger?.LogWarning("No weather data available - cannot run dispersion modeling");
                Dispatcher.Invoke(() =>
                {
                    _viewModel.StatusMessage = "Weather data not available for dispersion modeling";
                });
                return;
            }
            
            // Create release object from current settings
            var chemical = _viewModel.SelectedChemical ?? new Chemical 
            { 
                Name = _viewModel.ChemicalName,
                MolecularWeight = _viewModel.MolecularWeight,
                Density = _viewModel.Density,
                ToxicityThreshold = 100 // Default threshold
            };
            
            var release = new Release
            {
                Name = "Map Click Release",
                Latitude = latitude,
                Longitude = longitude,
                ReleaseHeight = _viewModel.ReleaseHeight,
                Chemical = chemical,
                ChemicalId = chemical.Id,
                ReleaseType = _viewModel.ReleaseType,
                ReleaseRate = _viewModel.ReleaseRate,
                TotalMass = _viewModel.TotalMass,
                StartTime = DateTime.Now,
                Scenario = _viewModel.ReleaseScenario,
                DiameterOrArea = 5.0, // Default 5m diameter
                InitialTemperature = weather.Temperature + 20, // Assume release is warmer
                ModelingWindSpeed = weather.WindSpeed,
                ModelingWindDirection = weather.WindDirection,
                ModelingStabilityClass = "D", // Neutral stability
                ModelingTemperature = weather.Temperature,
                ModelingHumidity = weather.Humidity
            };
            
            _logger?.LogInformation($"Release configuration: Rate={release.ReleaseRate}, Height={release.ReleaseHeight}, Chemical={chemical.Name}");
            
            // Calculate dispersion grid
            _logger?.LogInformation("Calculating dispersion grid...");
            var dispersionResults = await _dispersionService.CalculateDispersionGridAsync(
                release, 
                weather, 
                gridSize: 100,    // 100m grid resolution
                maxDistance: 5000 // 5km maximum distance
            );
            
            var resultsList = dispersionResults.ToList();
            _logger?.LogInformation($"Dispersion modeling completed: {resultsList.Count} grid points calculated");
            
            // Display the plume on the map
            if (resultsList.Any() && _realMappingService != null)
            {
                _logger?.LogInformation("Adding dispersion plume to map...");
                
                var dispersionVisualization = new DispersionVisualizationOptions
                {
                    ConcentrationLevels = new List<double> { 0.1, 1.0, 10.0, 100.0 }, // mg/m³
                    ContourColors = new List<string> { "#00FF00", "#FFFF00", "#FFA500", "#FF0000" },
                    Opacity = 0.7,
                    ShowConcentrationLabels = true,
                    ShowFootprint = true,
                    ShowCenterline = true
                };
                
                // Use the new grid visualization method
                if (_realMappingService is RealMappingService realMappingService)
                {
                    await realMappingService.AddDispersionGridAsync(resultsList, dispersionVisualization);
                    _logger?.LogInformation("Dispersion grid visualization added to map successfully");
                }
                else
                {
                    // Fallback to single result visualization
                    var combinedResult = new DispersionResult
                    {
                        Latitude = latitude,
                        Longitude = longitude,
                        Concentration = resultsList.Max(r => r.Concentration),
                        ConcentrationUnit = "mg/m³",
                        CalculationTime = DateTime.Now,
                        WindSpeed = weather.WindSpeed,
                        WindDirection = weather.WindDirection,
                        Temperature = weather.Temperature,
                        RiskLevel = resultsList.Where(r => r.Concentration > 1.0).Any() ? "HIGH" : "LOW"
                    };
                    
                    await _realMappingService.AddDispersionPlumeAsync(combinedResult, dispersionVisualization);
                    _logger?.LogInformation("Dispersion plume added to map successfully");
                }
                
                // Update status
                Dispatcher.Invoke(() =>
                {
                    var maxConc = resultsList.Max(r => r.Concentration);
                    var highRiskPoints = resultsList.Count(r => r.Concentration > chemical.ToxicityThreshold * 0.1);
                    _viewModel.StatusMessage = $"Dispersion calculated: Max concentration {maxConc:F2} mg/m³, {highRiskPoints} high-risk points";
                });
            }
            else
            {
                _logger?.LogWarning("No dispersion results generated or mapping service unavailable");
                Dispatcher.Invoke(() =>
                {
                    _viewModel.StatusMessage = "Dispersion modeling completed but no significant concentrations found";
                });
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error running dispersion modeling");
            Dispatcher.Invoke(() =>
            {
                if (_viewModel != null)
                {
                    _viewModel.StatusMessage = $"Dispersion modeling error: {ex.Message}";
                }
            });
        }
    }

    private void OnMapViewChanged(object? sender, MapViewChangedEventArgs e)
    {
        try
        {
            _logger?.LogInformation($"Map view changed: Center=({e.CenterLatitude}, {e.CenterLongitude}), Zoom={e.ZoomLevel}");
            
            // For now, just log the change - we can add map state tracking later
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling map view change");
        }
    }

    private string CreateDefaultMapHtml()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <title>Chemical Dispersion Map</title>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'
          integrity='sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY='
          crossorigin='' />
    <style>
        html, body { height: 100%; margin: 0; padding: 0; }
        #map { height: 100vh; width: 100%; }
        .plume-legend {
            position: absolute;
            top: 10px;
            right: 10px;
            background: white;
            padding: 10px;
            border-radius: 5px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.3);
            z-index: 1000;
            font-family: Arial, sans-serif;
            font-size: 12px;
        }
        .legend-item {
            margin: 2px 0;
            display: flex;
            align-items: center;
        }
        .legend-color {
            width: 20px;
            height: 15px;
            margin-right: 5px;
            border: 1px solid #ccc;
        }
    </style>
</head>
<body>
    <div id='map'></div>
    <div id='plume-legend' class='plume-legend' style='display: none;'>
        <div style='font-weight: bold; margin-bottom: 5px;'>Concentration (mg/m³)</div>
        <div id='legend-content'></div>
    </div>
    
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'
            integrity='sha256-20nQCchB9co0qIjJZRGuk2/Z9VM+kNiyxNV1lvTlZBo='
            crossorigin=''></script>
    <script>
        // Initialize map
        var map = L.map('map').setView([40.7831, -73.9712], 13);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(map);
        
        // Map state
        var currentMarker = null;
        var dispersionLayers = {};
        var plumeOverlays = L.layerGroup().addTo(map);
        
        // Color schemes for concentration visualization
        var colorSchemes = {
            'rainbow': ['#0000FF', '#00FFFF', '#00FF00', '#FFFF00', '#FF8000', '#FF0000'],
            'heat': ['#000080', '#0000FF', '#00FF00', '#FFFF00', '#FF8000', '#FF0000'],
            'grayscale': ['#000000', '#333333', '#666666', '#999999', '#CCCCCC', '#FFFFFF']
        };
        
        // Utility function to interpolate between colors
        function interpolateColor(color1, color2, factor) {
            var result = color1.slice();
            for (var i = 0; i < 3; i++) {
                result[i] = Math.round(result[i] + factor * (color2[i] - result[i]));
            }
            return result;
        }
        
        // Convert hex color to RGB array
        function hexToRgb(hex) {
            var result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
            return result ? [
                parseInt(result[1], 16),
                parseInt(result[2], 16),
                parseInt(result[3], 16)
            ] : null;
        }
        
        // Convert RGB array to hex
        function rgbToHex(rgb) {
            return '#' + ((1 << 24) + (rgb[0] << 16) + (rgb[1] << 8) + rgb[2]).toString(16).slice(1);
        }
        
        // Get color for concentration value
        function getConcentrationColor(concentration, maxConcentration, colors) {
            if (concentration <= 0) return 'transparent';
            
            var ratio = Math.min(concentration / maxConcentration, 1.0);
            var colorIndex = ratio * (colors.length - 1);
            var index1 = Math.floor(colorIndex);
            var index2 = Math.min(index1 + 1, colors.length - 1);
            var factor = colorIndex - index1;
            
            var rgb1 = hexToRgb(colors[index1]);
            var rgb2 = hexToRgb(colors[index2]);
            
            if (!rgb1 || !rgb2) return colors[index1];
            
            var interpolated = interpolateColor(rgb1, rgb2, factor);
            return rgbToHex(interpolated);
        }
        
        // Create contour polygons from grid data
        function createContourPolygons(gridData, contourLevels, colors, opacity) {
            var polygons = [];
            
            for (var i = 0; i < contourLevels.length; i++) {
                var level = contourLevels[i];
                var color = colors[i] || colors[colors.length - 1];
                
                // Find all grid points above this level
                var contourPoints = [];
                for (var j = 0; j < gridData.length; j++) {
                    var point = gridData[j];
                    if (point.concentration >= level) {
                        contourPoints.push([point.latitude, point.longitude, point.concentration]);
                    }
                }
                
                if (contourPoints.length > 0) {
                    // Create convex hull or use simplified contour
                    var polygon = createContourFromPoints(contourPoints, level);
                    if (polygon && polygon.length > 2) {
                        var leafletPolygon = L.polygon(polygon, {
                            color: color,
                            fillColor: color,
                            fillOpacity: opacity * 0.6,
                            opacity: opacity,
                            weight: 2
                        });
                        
                        // Add popup with concentration info
                        leafletPolygon.bindPopup(`Concentration: ≥ ${level.toFixed(2)} mg/m³`);
                        polygons.push(leafletPolygon);
                    }
                }
            }
            
            return polygons;
        }
        
        // Simplified contour creation from points
        function createContourFromPoints(points, level) {
            if (points.length < 3) return null;
            
            // Find bounding box
            var minLat = Math.min(...points.map(p => p[0]));
            var maxLat = Math.max(...points.map(p => p[0]));
            var minLng = Math.min(...points.map(p => p[1]));
            var maxLng = Math.max(...points.map(p => p[1]));
            
            // Create approximate contour polygon
            var margin = 0.001; // Approximate margin in degrees
            return [
                [minLat - margin, minLng - margin],
                [maxLat + margin, minLng - margin],
                [maxLat + margin, maxLng + margin],
                [minLat - margin, maxLng + margin],
                [minLat - margin, minLng - margin]
            ];
        }
        
        // Main function to add dispersion plume
        function addDispersionPlume(overlayId, data) {
            console.log('Adding dispersion plume:', overlayId, data);
            
            // Clear existing plume if it exists
            if (dispersionLayers[overlayId]) {
                clearDispersionPlume(overlayId);
            }
            
            var layers = [];
            
            // Create contour polygons if we have grid data
            if (data.gridData && data.gridData.length > 0) {
                var maxConc = Math.max(...data.gridData.map(p => p.concentration));
                var contourLevels = data.contourLevels || [maxConc * 0.1, maxConc * 0.3, maxConc * 0.6, maxConc];
                var colors = data.colors || colorSchemes.heat;
                var opacity = data.opacity || 0.7;
                
                var polygons = createContourPolygons(data.gridData, contourLevels, colors, opacity);
                layers = layers.concat(polygons);
                
                // Update legend
                updatePlumeeLegend(contourLevels, colors, data.unit || 'mg/m³');
                
                console.log(`Created ${polygons.length} contour polygons`);
            }
            
            // Create heat map style visualization for individual points
            if (data.coordinates && data.coordinates.length > 0) {
                for (var i = 0; i < data.coordinates.length; i++) {
                    var coord = data.coordinates[i];
                    var polygon = L.polygon(coord, {
                        color: data.color || '#FF0000',
                        fillColor: data.fillColor || data.color || '#FF0000',
                        fillOpacity: (data.opacity || 0.7) * 0.6,
                        opacity: data.opacity || 0.7,
                        weight: 2
                    });
                    
                    polygon.bindPopup(`Chemical Dispersion Zone<br/>Click for details`);
                    layers.push(polygon);
                }
            }
            
            // Add all layers to the map
            var layerGroup = L.layerGroup(layers);
            layerGroup.addTo(plumeOverlays);
            dispersionLayers[overlayId] = layerGroup;
            
            console.log(`Added ${layers.length} dispersion layers for overlay ${overlayId}`);
        }
        
        // Clear specific dispersion plume
        function clearDispersionPlume(overlayId) {
            if (dispersionLayers[overlayId]) {
                plumeOverlays.removeLayer(dispersionLayers[overlayId]);
                delete dispersionLayers[overlayId];
                console.log('Cleared dispersion plume:', overlayId);
            }
        }
        
        // Clear all dispersion overlays
        function clearAllDispersionPlumes() {
            Object.keys(dispersionLayers).forEach(function(overlayId) {
                clearDispersionPlume(overlayId);
            });
            hidePlumeeLegend();
            console.log('Cleared all dispersion plumes');
        }
        
        // Update plume legend
        function updatePlumeeLegend(levels, colors, unit) {
            var legend = document.getElementById('plume-legend');
            var content = document.getElementById('legend-content');
            
            content.innerHTML = '';
            
            for (var i = levels.length - 1; i >= 0; i--) {
                var item = document.createElement('div');
                item.className = 'legend-item';
                
                var colorBox = document.createElement('div');
                colorBox.className = 'legend-color';
                colorBox.style.backgroundColor = colors[i] || colors[colors.length - 1];
                
                var label = document.createElement('span');
                label.textContent = `≥ ${levels[i].toFixed(2)} ${unit}`;
                
                item.appendChild(colorBox);
                item.appendChild(label);
                content.appendChild(item);
            }
            
            legend.style.display = 'block';
        }
        
        // Hide plume legend
        function hidePlumeeLegend() {
            document.getElementById('plume-legend').style.display = 'none';
        }
        
        // Add release source marker
        function addReleaseMarker(lat, lng, data) {
            if (currentMarker) {
                map.removeLayer(currentMarker);
            }
            
            var icon = L.divIcon({
                className: 'release-marker',
                html: '<div style=""background-color: red; width: 12px; height: 12px; border-radius: 50%; border: 2px solid white; box-shadow: 0 2px 4px rgba(0,0,0,0.3);""></div>',
                iconSize: [16, 16],
                iconAnchor: [8, 8]
            });
            
            currentMarker = L.marker([lat, lng], { icon: icon }).addTo(map);
            
            var popupContent = `<strong>Release Source</strong><br/>`;
            popupContent += `Coordinates: ${lat.toFixed(6)}, ${lng.toFixed(6)}<br/>`;
            if (data && data.chemical) {
                popupContent += `Chemical: ${data.chemical}<br/>`;
            }
            if (data && data.rate) {
                popupContent += `Release Rate: ${data.rate} kg/s<br/>`;
            }
            
            currentMarker.bindPopup(popupContent);
        }
        
        // Map click handler
        map.on('click', function(e) {
            addReleaseMarker(e.latlng.lat, e.latlng.lng);
            
            var message = {
                type: 'mapClick',
                latitude: e.latlng.lat,
                longitude: e.latlng.lng,
                screenX: e.containerPoint.x,
                screenY: e.containerPoint.y
            };
            
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(JSON.stringify(message));
            }
        });
        
        // Expose functions globally for C# integration
        window.addDispersionPlume = addDispersionPlume;
        window.clearDispersionPlume = clearDispersionPlume;
        window.clearAllDispersionPlumes = clearAllDispersionPlumes;
        window.addReleaseMarker = addReleaseMarker;
        
        console.log('Chemical Dispersion Map initialized with plume visualization');
    </script>
</body>
</html>";
    }

    protected override void OnClosed(EventArgs e)
    {
        try
        {
            // Stop live services
            _liveWeatherService?.StopContinuousMonitoringAsync();
            _gasSensorService?.StopContinuousMonitoringAsync();
            
            // Unsubscribe from events
            if (_liveWeatherService != null)
                _liveWeatherService.WeatherDataReceived -= OnWeatherDataReceived;
            
            if (_gasSensorService != null)
                _gasSensorService.GasSensorDataReceived -= OnGasSensorDataReceived;
                
            if (_realMappingService is RealMappingService realMapping)
            {
                realMapping.MapClicked -= OnMapClicked;
                realMapping.MapViewChanged -= OnMapViewChanged;
            }
            
            _logger?.LogInformation("MainWindow closed and resources cleaned up");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during window cleanup");
        }
        
        base.OnClosed(e);
    }
}