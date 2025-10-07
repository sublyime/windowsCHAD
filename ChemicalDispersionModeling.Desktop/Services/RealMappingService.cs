using ChemicalDispersionModeling.Core.Models;
using ChemicalDispersionModeling.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Text.Json;

namespace ChemicalDispersionModeling.Desktop.Services;

public class RealMappingService : IRealMappingService
{
    private readonly ILogger<RealMappingService> _logger;
    private WebView2? _webView;
    private MapProvider _currentProvider;
    private string _apiKey = "";
    private double _currentLatitude;
    private double _currentLongitude;
    private int _currentZoom = 10;
    private readonly List<string> _dispersionOverlays = new();

    public event EventHandler<MapClickEventArgs>? MapClicked;
    public event EventHandler<MapViewChangedEventArgs>? MapViewChanged;

    public RealMappingService(ILogger<RealMappingService> logger)
    {
        _logger = logger;
    }

    public void SetWebView(WebView2 webView)
    {
        _webView = webView;
        _webView.NavigationCompleted += OnNavigationCompleted;
        _webView.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;
        
        Console.WriteLine("=== SetWebView called ===");
        Console.WriteLine($"CoreWebView2 already initialized: {_webView.CoreWebView2 != null}");
        
        // If CoreWebView2 is already initialized, manually attach the handler
        if (_webView.CoreWebView2 != null)
        {
            Console.WriteLine("=== CoreWebView2 already ready, attaching handler manually ===");
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            Console.WriteLine("=== C# WebMessageReceived handler attached manually! ===");
        }
    }

    public async Task<bool> InitializeAsync(MapProvider provider, string apiKey = "")
    {
        try
        {
            if (_webView == null)
            {
                _logger.LogError("WebView2 not set before initialization");
                return false;
            }

            _currentProvider = provider;
            _apiKey = apiKey;

            _logger.LogInformation($"Initializing real mapping service with {provider}");
            Console.WriteLine($"=== InitializeAsync called ===");
            Console.WriteLine($"CoreWebView2 before EnsureCoreWebView2Async: {_webView.CoreWebView2 != null}");

            await _webView.EnsureCoreWebView2Async();
            Console.WriteLine($"CoreWebView2 after EnsureCoreWebView2Async: {_webView.CoreWebView2 != null}");
            
            // Double-check that the handler is attached after initialization
            if (_webView.CoreWebView2 != null)
            {
                Console.WriteLine("=== Double-checking WebMessageReceived handler after EnsureCoreWebView2Async ===");
                // Remove any existing handler first to avoid duplicates
                _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                Console.WriteLine("=== C# WebMessageReceived handler attached/re-attached! ===");
            }

            var htmlContent = provider switch
            {
                MapProvider.OpenStreetMap => GenerateOpenStreetMapHTML(),
                MapProvider.GoogleMaps => GenerateGoogleMapsHTML(),
                MapProvider.BingMaps => GenerateBingMapsHTML(),
                MapProvider.MapBox => GenerateMapBoxHTML(),
                _ => GenerateOpenStreetMapHTML() // Default to OSM
            };

            _webView.NavigateToString(htmlContent);
            
            _logger.LogInformation($"Real mapping service initialized with {provider}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize mapping service with {provider}");
            return false;
        }
    }

    public async Task SetMapViewAsync(double latitude, double longitude, int zoomLevel)
    {
        try
        {
            if (_webView?.CoreWebView2 == null) return;

            _currentLatitude = latitude;
            _currentLongitude = longitude;
            _currentZoom = zoomLevel;

            var script = $"setMapView({latitude}, {longitude}, {zoomLevel});";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
            
            _logger.LogInformation($"Map view set to {latitude}, {longitude} zoom {zoomLevel}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting map view");
        }
    }

    public async Task AddDispersionPlumeAsync(DispersionResult result, DispersionVisualizationOptions options)
    {
        try
        {
            if (_webView?.CoreWebView2 == null) return;

            // Clear existing overlays if requested
            await ClearDispersionOverlaysAsync();

            // Generate contour data from dispersion result
            var contourData = GenerateContourData(result, options);
            var overlayId = Guid.NewGuid().ToString();

            var script = $"addDispersionPlume('{overlayId}', {JsonSerializer.Serialize(contourData)});";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);

            _dispersionOverlays.Add(overlayId);
            
            _logger.LogInformation($"Added dispersion plume overlay: {overlayId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding dispersion plume");
        }
    }

    public async Task ClearDispersionOverlaysAsync()
    {
        try
        {
            if (_webView?.CoreWebView2 == null) return;

            foreach (var overlayId in _dispersionOverlays)
            {
                var script = $"removeDispersionPlume('{overlayId}');";
                await _webView.CoreWebView2.ExecuteScriptAsync(script);
            }

            _dispersionOverlays.Clear();
            _logger.LogInformation("Cleared all dispersion overlays");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing dispersion overlays");
        }
    }

    public async Task ClearReleaseMarkersAsync()
    {
        try
        {
            if (_webView?.CoreWebView2 == null) return;

            var script = "clearReleaseMarkers();";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
            
            _logger.LogInformation("Cleared all release markers");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing release markers");
        }
    }

    public async Task AddWeatherStationAsync(WeatherStation station)
    {
        try
        {
            if (_webView?.CoreWebView2 == null) return;

            var markerData = new
            {
                id = station.Id,
                lat = station.Latitude,
                lng = station.Longitude,
                title = station.Name,
                type = "weather",
                data = station.CurrentWeather,
                isOnline = station.IsOnline
            };

            var script = $"addMarker({JsonSerializer.Serialize(markerData)});";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
            
            _logger.LogInformation($"Added weather station marker: {station.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding weather station");
        }
    }

    public async Task AddGasSensorAsync(GasSensor sensor)
    {
        try
        {
            if (_webView?.CoreWebView2 == null) return;

            var markerData = new
            {
                id = sensor.Id,
                lat = sensor.Latitude,
                lng = sensor.Longitude,
                title = sensor.Name,
                type = "gas_sensor",
                data = sensor.LatestReading,
                isOnline = sensor.IsOnline,
                chemicals = sensor.DetectedChemicals
            };

            var script = $"addMarker({JsonSerializer.Serialize(markerData)});";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
            
            _logger.LogInformation($"Added gas sensor marker: {sensor.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding gas sensor");
        }
    }

    public async Task AddReleaseSourceAsync(Release release)
    {
        try
        {
            if (_webView?.CoreWebView2 == null) return;

            var markerData = new
            {
                id = release.Id.ToString(),
                lat = release.Latitude,
                lng = release.Longitude,
                title = $"Release: {release.Chemical?.Name}",
                type = "release",
                data = release
            };

            var script = $"addMarker({JsonSerializer.Serialize(markerData)});";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
            
            _logger.LogInformation($"Added release source marker: {release.Chemical?.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding release source");
        }
    }

    public async Task AddReceptorAsync(Receptor receptor)
    {
        try
        {
            if (_webView?.CoreWebView2 == null) return;

            var markerData = new
            {
                id = receptor.Id.ToString(),
                lat = receptor.Latitude,
                lng = receptor.Longitude,
                title = receptor.Name,
                type = "receptor",
                data = receptor
            };

            var script = $"addMarker({JsonSerializer.Serialize(markerData)});";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
            
            _logger.LogInformation($"Added receptor marker: {receptor.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding receptor");
        }
    }

    public async Task<TerrainData> LoadTerrainDataAsync(double minLat, double minLon, double maxLat, double maxLon)
    {
        try
        {
            _logger.LogInformation($"Loading terrain data for bounds: {minLat},{minLon} to {maxLat},{maxLon}");

            // In a real implementation, this would call elevation APIs
            var terrainData = new TerrainData
            {
                Latitude = (minLat + maxLat) / 2,
                Longitude = (minLon + maxLon) / 2,
                Elevation = 100, // Default elevation
                LandUseType = "Urban",
                SurfaceRoughness = 0.3,
                DataSource = "USGS DEM"
            };

            // Simulate loading terrain data
            await Task.Delay(100);
            
            return terrainData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading terrain data");
            throw;
        }
    }

    public async Task LoadBuildingDataAsync(double minLat, double minLon, double maxLat, double maxLon)
    {
        try
        {
            if (_webView?.CoreWebView2 == null) return;

            _logger.LogInformation($"Loading building data for bounds: {minLat},{minLon} to {maxLat},{maxLon}");

            // In a real implementation, this would call building footprint APIs
            var script = $"loadBuildingData({minLat}, {minLon}, {maxLat}, {maxLon});";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading building data");
        }
    }

    public async Task<(double latitude, double longitude)> ScreenToGeographicAsync(double x, double y)
    {
        try
        {
            if (_webView?.CoreWebView2 == null) return (0, 0);

            var script = $"screenToGeographic({x}, {y});";
            var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
            
            var coords = JsonSerializer.Deserialize<double[]>(result);
            return (coords?[0] ?? 0, coords?[1] ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting screen to geographic coordinates");
            return (0, 0);
        }
    }

    public async Task<(double x, double y)> GeographicToScreenAsync(double latitude, double longitude)
    {
        try
        {
            if (_webView?.CoreWebView2 == null) return (0, 0);

            var script = $"geographicToScreen({latitude}, {longitude});";
            var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
            
            var coords = JsonSerializer.Deserialize<double[]>(result);
            return (coords?[0] ?? 0, coords?[1] ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting geographic to screen coordinates");
            return (0, 0);
        }
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        _logger.LogInformation($"Map navigation completed successfully: {e.IsSuccess}");
    }

    private void OnCoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        Console.WriteLine("=== OnCoreWebView2InitializationCompleted called ===");
        System.Diagnostics.Debug.WriteLine("=== OnCoreWebView2InitializationCompleted called ===");
        
        if (e.IsSuccess && _webView?.CoreWebView2 != null)
        {
            // Add JavaScript bridge for map events
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            Console.WriteLine("=== C# WebMessageReceived handler attached! ===");
            System.Diagnostics.Debug.WriteLine("=== C# WebMessageReceived handler attached! ===");
            
            // Show a message box to confirm initialization
            System.Windows.MessageBox.Show("WebView2 initialized and WebMessageReceived handler attached!", 
                "Initialization Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            
            _logger.LogInformation("WebView2 initialization completed successfully");
        }
        else
        {
            Console.WriteLine($"=== WebView2 initialization failed: {e.InitializationException?.Message} ===");
            System.Diagnostics.Debug.WriteLine($"=== WebView2 initialization failed: {e.InitializationException?.Message} ===");
            
            System.Windows.MessageBox.Show($"WebView2 initialization failed: {e.InitializationException?.Message}", 
                "Initialization Failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            
            _logger.LogError($"WebView2 initialization failed: {e.InitializationException?.Message}");
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // Use multiple output methods to ensure we see the message
        Console.WriteLine("=== C# WebMessage Received! ===");
        System.Diagnostics.Debug.WriteLine("=== C# WebMessage Received! ===");
        
        try
        {
            // Try different ways to get the message
            string message;
            try 
            {
                message = e.TryGetWebMessageAsString();
            }
            catch (Exception ex1)
            {
                Console.WriteLine($"TryGetWebMessageAsString failed: {ex1.Message}");
                try 
                {
                    message = e.WebMessageAsJson;
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"WebMessageAsJson failed: {ex2.Message}");
                    System.Windows.MessageBox.Show($"Failed to get message content!\nTryGetWebMessageAsString: {ex1.Message}\nWebMessageAsJson: {ex2.Message}", 
                        "Message Retrieval Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
            }
            
            Console.WriteLine($"Message: {message}");
            System.Diagnostics.Debug.WriteLine($"Message: {message}");
            
            // Parse JSON more carefully
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;
            
            if (root.TryGetProperty("type", out var typeElement))
            {
                var messageType = typeElement.GetString();
                Console.WriteLine($"Message type: {messageType}");
                System.Diagnostics.Debug.WriteLine($"Message type: {messageType}");
                
                switch (messageType)
                {
                    case "mapClick":
                        Console.WriteLine("Calling HandleMapClick");
                        HandleMapClick(root);
                        break;
                    case "mapViewChanged":
                        HandleMapViewChanged(root);
                        break;
                    case "test":
                        Console.WriteLine("=== Test message received from JavaScript! ===");
                        System.Diagnostics.Debug.WriteLine("=== Test message received from JavaScript! ===");
                        if (root.TryGetProperty("message", out var testMessage))
                        {
                            Console.WriteLine($"Test message content: {testMessage.GetString()}");
                            System.Diagnostics.Debug.WriteLine($"Test message content: {testMessage.GetString()}");
                        }
                        
                        // Show success message for test
                        System.Windows.MessageBox.Show($"Test message received successfully!\nMessage: {testMessage.GetString()}", 
                            "Communication Test Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            _logger.LogError(ex, "Error processing web message");
            
            // Show error in message box too
            System.Windows.MessageBox.Show($"WebMessage Error!\nError: {ex.Message}\nStack: {ex.StackTrace}", 
                "Debug Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void HandleMapClick(JsonElement data)
    {
        if (data.TryGetProperty("lat", out var latElement) && 
            data.TryGetProperty("lng", out var lngElement))
        {
            var latitude = latElement.GetDouble();
            var longitude = lngElement.GetDouble();
            
            var args = new MapClickEventArgs
            {
                Latitude = latitude,
                Longitude = longitude,
                ScreenX = data.TryGetProperty("x", out var xElement) ? xElement.GetDouble() : 0,
                ScreenY = data.TryGetProperty("y", out var yElement) ? yElement.GetDouble() : 0,
                IsRightClick = data.TryGetProperty("rightClick", out var rightClickElement) && rightClickElement.GetBoolean()
            };

            Console.WriteLine($"=== Triggering MapClicked event for coordinates: {args.Latitude:F6}, {args.Longitude:F6} ===");
            Console.WriteLine($"Release marker should already be added by JavaScript");
            
            MapClicked?.Invoke(this, args);
            
            // Show success message
            System.Windows.MessageBox.Show($"Map Click Processed!\nLat: {args.Latitude:F6}\nLng: {args.Longitude:F6}\nRelease marker updated!", 
                "Map Click Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }

    private void HandleMapViewChanged(JsonElement data)
    {
        if (data.TryGetProperty("centerLat", out var centerLatElement) &&
            data.TryGetProperty("centerLng", out var centerLngElement) &&
            data.TryGetProperty("zoom", out var zoomElement))
        {
            var args = new MapViewChangedEventArgs
            {
                CenterLatitude = centerLatElement.GetDouble(),
                CenterLongitude = centerLngElement.GetDouble(),
                ZoomLevel = zoomElement.GetInt32(),
                MinLatitude = data.TryGetProperty("minLat", out var minLatElement) ? minLatElement.GetDouble() : 0,
                MinLongitude = data.TryGetProperty("minLng", out var minLngElement) ? minLngElement.GetDouble() : 0,
                MaxLatitude = data.TryGetProperty("maxLat", out var maxLatElement) ? maxLatElement.GetDouble() : 0,
                MaxLongitude = data.TryGetProperty("maxLng", out var maxLngElement) ? maxLngElement.GetDouble() : 0
            };

            MapViewChanged?.Invoke(this, args);
        }
    }

    private string GenerateOpenStreetMapHTML()
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Chemical Dispersion Map</title>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        html, body {{ height: 100%; margin: 0; padding: 0; }}
        #map {{ height: 100%; width: 100%; }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        var map = L.map('map').setView([39.8283, -98.5795], 5);
        
        L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
            attribution: '© OpenStreetMap contributors'
        }}).addTo(map);

        var markers = {{}};
        var overlays = {{}};
        var releaseMarker = null; // Track the current release point marker

        function setMapView(lat, lng, zoom) {{
            map.setView([lat, lng], zoom);
        }}

        function addMarker(data) {{
            var marker = L.marker([data.lat, data.lng])
                .addTo(map)
                .bindPopup(data.title);
            markers[data.id] = marker;
        }}

        function addReleaseMarker(lat, lng, title) {{
            // Remove existing release marker
            if (releaseMarker) {{
                map.removeLayer(releaseMarker);
            }}
            
            // Add new release marker with distinctive red marker
            releaseMarker = L.marker([lat, lng]).addTo(map).bindPopup(title || 'Chemical Release Point');
            console.log('Added release marker at:', lat, lng);
        }}

        function clearReleaseMarkers() {{
            if (releaseMarker) {{
                map.removeLayer(releaseMarker);
                releaseMarker = null;
                console.log('Cleared release marker');
            }}
        }}

        function addDispersionPlume(id, contourData) {{
            var polygon = L.polygon(contourData.coordinates, {{
                color: contourData.color,
                fillColor: contourData.fillColor,
                fillOpacity: contourData.opacity
            }}).addTo(map);
            overlays[id] = polygon;
        }}

        function removeDispersionPlume(id) {{
            if (overlays[id]) {{
                map.removeLayer(overlays[id]);
                delete overlays[id];
            }}
        }}

        function loadBuildingData(minLat, minLng, maxLat, maxLng) {{
            // Load building footprints from Overpass API
            console.log('Loading building data for bounds:', minLat, minLng, maxLat, maxLng);
        }}

        function screenToGeographic(x, y) {{
            var point = map.containerPointToLatLng([x, y]);
            return [point.lat, point.lng];
        }}

        function geographicToScreen(lat, lng) {{
            var point = map.latLngToContainerPoint([lat, lng]);
            return [point.x, point.y];
        }}

        // Event handlers
        map.on('click', function(e) {{
            console.log('=== JavaScript map click detected! ===');
            console.log('Coordinates:', e.latlng.lat, e.latlng.lng);
            console.log('Container point:', e.containerPoint.x, e.containerPoint.y);
            
            // Immediately add/update the release marker
            addReleaseMarker(e.latlng.lat, e.latlng.lng, 'Chemical Release Point');
            
            // Check if postMessage is available
            if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {{
                console.log('=== Sending postMessage to C# ===');
                window.chrome.webview.postMessage({{
                    type: 'mapClick',
                    lat: e.latlng.lat,
                    lng: e.latlng.lng,
                    x: e.containerPoint.x,
                    y: e.containerPoint.y
                }});
                console.log('=== PostMessage sent successfully ===');
            }} else {{
                console.error('=== window.chrome.webview.postMessage not available! ===');
                console.log('window.chrome:', window.chrome);
                console.log('window.chrome.webview:', window.chrome ? window.chrome.webview : 'undefined');
            }}
        }});

        // Test if WebView2 communication is ready
        console.log('=== JavaScript loaded and ready ===');
        console.log('window.chrome available:', !!window.chrome);
        console.log('window.chrome.webview available:', !!(window.chrome && window.chrome.webview));
        console.log('postMessage available:', !!(window.chrome && window.chrome.webview && window.chrome.webview.postMessage));
        
        // Send a test message immediately after load
        setTimeout(function() {{
            if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {{
                console.log('=== Sending test message after 1 second ===');
                window.chrome.webview.postMessage({{
                    type: 'test',
                    message: 'JavaScript communication test'
                }});
            }}
        }}, 1000);

        map.on('moveend zoomend', function(e) {{
            var bounds = map.getBounds();
            window.chrome.webview.postMessage({{
                type: 'mapViewChanged',
                centerLat: map.getCenter().lat,
                centerLng: map.getCenter().lng,
                zoom: map.getZoom(),
                minLat: bounds.getSouth(),
                minLng: bounds.getWest(),
                maxLat: bounds.getNorth(),
                maxLng: bounds.getEast()
            }});
        }});
    </script>
</body>
</html>";
    }

    private string GenerateGoogleMapsHTML()
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Chemical Dispersion Map</title>
    <style>
        html, body {{ height: 100%; margin: 0; padding: 0; }}
        #map {{ height: 100%; width: 100%; }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        // Google Maps implementation would go here
        console.log('Google Maps not implemented yet');
    </script>
</body>
</html>";
    }

    private string GenerateBingMapsHTML()
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Chemical Dispersion Map</title>
    <style>
        html, body {{ height: 100%; margin: 0; padding: 0; }}
        #map {{ height: 100%; width: 100%; }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        // Bing Maps implementation would go here
        console.log('Bing Maps not implemented yet');
    </script>
</body>
</html>";
    }

    private string GenerateMapBoxHTML()
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Chemical Dispersion Map</title>
    <style>
        html, body {{ height: 100%; margin: 0; padding: 0; }}
        #map {{ height: 100%; width: 100%; }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        // MapBox implementation would go here
        console.log('MapBox not implemented yet');
    </script>
</body>
</html>";
    }

    private object GenerateContourData(DispersionResult result, DispersionVisualizationOptions options)
    {
        // Generate contour polygons from dispersion result
        // This is a simplified version - real implementation would use contouring algorithms
        var coordinates = new List<List<double[]>>();
        
        // Create sample contour for demonstration
        var centerLat = result.Release?.Latitude ?? _currentLatitude;
        var centerLng = result.Release?.Longitude ?? _currentLongitude;
        
        var contour = new List<double[]>
        {
            new[] { centerLat + 0.01, centerLng - 0.01 },
            new[] { centerLat + 0.01, centerLng + 0.01 },
            new[] { centerLat - 0.01, centerLng + 0.01 },
            new[] { centerLat - 0.01, centerLng - 0.01 },
            new[] { centerLat + 0.01, centerLng - 0.01 }
        };
        
        coordinates.Add(contour);
        
        return new
        {
            coordinates = coordinates,
            color = options.ContourColors.FirstOrDefault() ?? "#FF0000",
            fillColor = options.ContourColors.FirstOrDefault() ?? "#FF0000",
            opacity = options.Opacity
        };
    }
}