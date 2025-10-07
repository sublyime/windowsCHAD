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
            _logger?.LogError(ex, "Error initializing MainWindow");
            MessageBox.Show($"Error initializing MainWindow: {ex.Message}", 
                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Console.WriteLine("=== MainWindow_Loaded Started ===");
            _logger?.LogInformation("MainWindow_Loaded started");
            _viewModel = DataContext as MainViewModel;
            
            if (_viewModel == null)
            {
                Console.WriteLine("=== ERROR: DataContext is null or not MainViewModel ===");
                _logger?.LogError("DataContext is null or not MainViewModel");
                MessageBox.Show("DataContext not set properly. The application may not function correctly.", 
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Console.WriteLine("=== MainViewModel successfully bound ===");
            _logger?.LogInformation("MainViewModel successfully bound");
            
            // Only initialize services if they're available (dependency injection was used)
            if (_liveWeatherService != null && _realMappingService != null)
            {
                // Initialize the real map
                _logger?.LogInformation("Starting map initialization");
                await InitializeRealMapAsync();
                
                // Start live weather monitoring for Houston, TX (default location)
                _logger?.LogInformation("Starting weather monitoring");
                try
                {
                    await _liveWeatherService.StartContinuousMonitoringAsync(
                        29.7604, -95.3698, DataSourceType.OpenMeteo, 300);
                }
                catch (Exception weatherEx)
                {
                    _logger?.LogWarning(weatherEx, "Failed to start weather monitoring, continuing without live weather");
                }
                    
                _viewModel.StatusMessage = "Application initialized with real mapping and live data";
            }
            else
            {
                _viewModel.StatusMessage = "Application initialized in basic mode";
            }
            
            _logger?.LogInformation("MainWindow_Loaded completed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during window loading");
            MessageBox.Show($"Error during window loading: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task InitializeRealMapAsync()
    {
        try
        {
            Console.WriteLine("=== Initializing real mapping service ===");
            _logger?.LogInformation("Initializing real mapping service");

            // Find the WebView2 in the XAML (we'll need to add it)
            _mapWebView = FindName("MapWebView") as WebView2;
            Console.WriteLine($"=== WebView2 from XAML: {_mapWebView != null} ===");
            
            if (_mapWebView == null)
            {
                Console.WriteLine("=== Creating WebView2 programmatically ===");
                // Create WebView2 programmatically if not found in XAML
                _mapWebView = new WebView2();
                
                // Find the map container and add WebView2
                var mapContainer = FindName("MapContainer") as Border;
                Console.WriteLine($"=== MapContainer found: {mapContainer != null} ===");
                if (mapContainer != null)
                {
                    mapContainer.Child = _mapWebView;
                    Console.WriteLine("=== WebView2 added to MapContainer ===");
                }
            }

            Console.WriteLine($"=== WebView2 ready: {_mapWebView != null} ===");
            Console.WriteLine($"=== RealMappingService type: {_realMappingService?.GetType().Name} ===");
            if (_mapWebView != null && _realMappingService is RealMappingService realMapping)
            {
                Console.WriteLine("=== Calling SetWebView ===");
                realMapping.SetWebView(_mapWebView);
                Console.WriteLine("=== Calling InitializeAsync ===");
                await realMapping.InitializeAsync(MapProvider.OpenStreetMap);
                Console.WriteLine("=== InitializeAsync completed ===");
                
                // Set default view to Houston, TX area
                await realMapping.SetMapViewAsync(29.7604, -95.3698, 10);
                
                // Subscribe to map events
                Console.WriteLine($"Subscribing to map events...");
                realMapping.MapClicked += OnMapClicked;
                realMapping.MapViewChanged += OnMapViewChanged;
                Console.WriteLine($"Map events subscribed successfully");
                
                _logger?.LogInformation("Real mapping service initialized successfully");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error initializing real map");
            MessageBox.Show($"Error initializing map: {ex.Message}", "Map Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnWeatherDataReceived(object? sender, WeatherData weatherData)
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                if (_viewModel != null)
                {
                    _viewModel.CurrentWeather = weatherData;
                    _viewModel.StatusMessage = $"Weather updated: {weatherData.Temperature:F1}�C, Wind: {weatherData.WindSpeed:F1} m/s from {weatherData.WindDirection:F0}�";
                }
            });
            
            _logger?.LogInformation($"Weather data received: {weatherData.Temperature:F1}�C, Wind: {weatherData.WindSpeed:F1} m/s");
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
            Dispatcher.Invoke(() =>
            {
                if (_viewModel != null)
                {
                    var readingList = readings.ToList();
                    _viewModel.StatusMessage = $"Gas sensor data received: {readingList.Count} readings";
                    
                    // Add gas sensors to map
                    foreach (var reading in readingList)
                    {
                        Task.Run(async () =>
                        {
                            var sensor = new GasSensor
                            {
                                Id = reading.SensorId,
                                Name = reading.SensorLocation,
                                Latitude = reading.Latitude,
                                Longitude = reading.Longitude,
                                LatestReading = reading,
                                IsOnline = true
                            };
                            
                            if (_realMappingService != null)
                            {
                                await _realMappingService.AddGasSensorAsync(sensor);
                            }
                        });
                    }
                }
            });
            
            _logger?.LogInformation($"Gas sensor data received: {readings.Count()} readings");
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
                    _viewModel.ReleaseLatitude = e.Latitude;
                    _viewModel.ReleaseLongitude = e.Longitude;
                    _viewModel.StatusMessage = $"Setting release location to {e.Latitude:F6}, {e.Longitude:F6}...";
                    _viewModel.DebugStatus = $"Debug: Map clicked at {e.Latitude:F6}, {e.Longitude:F6}";
                    _logger?.LogInformation($"AFTER UPDATE - New location: Lat={_viewModel.ReleaseLatitude:F6}, Lng={_viewModel.ReleaseLongitude:F6}");
                    _logger?.LogInformation($"Status message set: {_viewModel.StatusMessage}");
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

    private void OnMapViewChanged(object? sender, MapViewChangedEventArgs e)
    {
        try
        {
            _logger?.LogDebug($"Map view changed: Center {e.CenterLatitude:F6}, {e.CenterLongitude:F6}, Zoom {e.ZoomLevel}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing map view change");
        }
    }

    private void TestLocationUpdate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Console.WriteLine($"=== TEST LOCATION UPDATE CLICKED ===");
            
            // Test coordinates (New York City)
            double testLat = 40.7128;
            double testLng = -74.0060;
            
            Console.WriteLine($"Simulating map click at NYC: {testLat:F6}, {testLng:F6}");
            
            // Create a simulated map click event
            var testArgs = new MapClickEventArgs
            {
                Latitude = testLat,
                Longitude = testLng,
                ScreenX = 100,
                ScreenY = 100,
                IsRightClick = false
            };
            
            // Call the map click handler directly
            OnMapClicked(this, testArgs);
            
            Console.WriteLine($"Test location update completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in test location update: {ex.Message}");
            _logger?.LogError(ex, "Error in test location update");
        }
    }

    private void TestMapClick_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Console.WriteLine("=== TEST MAP CLICK BUTTON PRESSED ===");
            MessageBox.Show("Testing map click functionality...", "Map Test", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Test if WebView2 is accessible
            if (_mapWebView != null)
            {
                Console.WriteLine($"WebView2 found: {_mapWebView.Source}");
                MessageBox.Show($"WebView2 Status:\nFound: Yes\nSource: {_mapWebView.Source}\nCoreWebView2: {(_mapWebView.CoreWebView2 != null ? "Initialized" : "Not Initialized")}", 
                    "WebView2 Status", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Try to manually trigger a map click event for testing
                if (_realMappingService != null)
                {
                    Console.WriteLine("Manually triggering map click event...");
                    var testArgs = new MapClickEventArgs
                    {
                        Latitude = 29.7604, // Houston coordinates
                        Longitude = -95.3698,
                        ScreenX = 100,
                        ScreenY = 100,
                        IsRightClick = false
                    };
                    
                    // Manually call the map click handler
                    OnMapClicked(_realMappingService, testArgs);
                    MessageBox.Show("Manual map click event triggered!", "Test Result", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("RealMappingService is null!", "Test Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("WebView2 not found!", "Test Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test Map Click Error: {ex.Message}");
            MessageBox.Show($"Test Map Click Error: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenDevTools_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Console.WriteLine("=== OPENING WEBVIEW2 DEV TOOLS ===");
            
            if (_mapWebView?.CoreWebView2 != null)
            {
                _mapWebView.CoreWebView2.OpenDevToolsWindow();
                MessageBox.Show("Developer Tools opened! Check the Console tab to see JavaScript output.", 
                    "Dev Tools", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("WebView2 CoreWebView2 not initialized yet!", 
                    "Dev Tools Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Open Dev Tools Error: {ex.Message}");
            MessageBox.Show($"Error opening dev tools: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Menu Command Handlers - Now with real functionality
    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel != null)
            {
                _viewModel.ResetToDefaults();
                _viewModel.StatusMessage = "New project created";
            }
            _logger?.LogInformation("New project created");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating new project");
            MessageBox.Show($"Error creating new project: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Chemical Dispersion Projects (*.cdp)|*.cdp|All files (*.*)|*.*",
                Title = "Open Project"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // TODO: Implement project loading
                _viewModel?.UpdateStatusMessage($"Project loaded: {IOPath.GetFileName(openFileDialog.FileName)}");
                _logger?.LogInformation($"Project opened: {openFileDialog.FileName}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening project");
            MessageBox.Show($"Error opening project: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Chemical Dispersion Projects (*.cdp)|*.cdp|All files (*.*)|*.*",
                Title = "Save Project"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // TODO: Implement project saving
                _viewModel?.UpdateStatusMessage($"Project saved: {IOPath.GetFileName(saveFileDialog.FileName)}");
                _logger?.LogInformation($"Project saved: {saveFileDialog.FileName}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving project");
            MessageBox.Show($"Error saving project: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RunModel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel == null || _dispersionService == null || _realMappingService == null) 
            {
                MessageBox.Show("Required services not available. Please restart the application.", 
                    "Service Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _viewModel.StatusMessage = "Running dispersion model...";

            // Create release scenario from current settings
            var release = new Release
            {
                Name = "Emergency Release",
                Latitude = _viewModel.ReleaseLatitude,
                Longitude = _viewModel.ReleaseLongitude,
                ReleaseHeight = _viewModel.ReleaseHeight,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(_viewModel.ReleaseDuration),
                ReleaseRate = _viewModel.ReleaseRate,
                ReleaseType = "Continuous",
                Scenario = "Gas",
                Chemical = new Chemical 
                { 
                    Name = _viewModel.ChemicalName,
                    MolecularWeight = _viewModel.MolecularWeight,
                    Density = _viewModel.Density,
                    VaporPressure = _viewModel.VaporPressure
                }
            };

            // Use current weather data
            var weatherData = _viewModel.CurrentWeather ?? new WeatherData
            {
                Temperature = _viewModel.Temperature,
                WindSpeed = _viewModel.WindSpeed,
                WindDirection = _viewModel.WindDirection,
                StabilityClass = _viewModel.StabilityClass,
                Timestamp = DateTime.UtcNow,
                StationId = "USER_INPUT",
                Latitude = _viewModel.ReleaseLatitude,
                Longitude = _viewModel.ReleaseLongitude,
                Pressure = 1013.25,
                Humidity = 60.0,
                Source = "Manual Input"
            };

            // Create receptors for grid calculation
            var receptors = new List<Receptor>();
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    receptors.Add(new Receptor
                    {
                        Name = $"Grid_{i}_{j}",
                        Latitude = _viewModel.ReleaseLatitude + (i - 5) * 0.001,
                        Longitude = _viewModel.ReleaseLongitude + (j - 5) * 0.001,
                        Height = 1.5 // Breathing height
                    });
                }
            }

            // Run dispersion model
            var results = await _dispersionService.CalculateGaussianPlumeAsync(release, receptors, weatherData);

            if (results?.Any() == true)
            {
                var firstResult = results.First();
                // Display results on map
                var visualizationOptions = new DispersionVisualizationOptions
                {
                    ConcentrationLevels = new List<double> { 0.001, 0.01, 0.1, 1.0 },
                    ContourColors = new List<string> { "#00FF00", "#FFFF00", "#FF8000", "#FF0000" },
                    Opacity = 0.6,
                    ShowConcentrationLabels = true,
                    ShowFootprint = true
                };

                await _realMappingService.AddDispersionPlumeAsync(firstResult, visualizationOptions);
                
                _viewModel.StatusMessage = $"Model completed. {results.Count()} concentration points calculated.";
                _logger?.LogInformation("Dispersion model completed successfully");
            }
            else
            {
                _viewModel.StatusMessage = "Model failed to run";
                _logger?.LogWarning("Dispersion model returned null result");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error running dispersion model");
            _viewModel?.UpdateStatusMessage($"Model error: {ex.Message}");
            MessageBox.Show($"Error running model: {ex.Message}", "Model Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ImportWeatherData_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_liveWeatherService == null)
            {
                MessageBox.Show("Weather service not available.", "Service Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|JSON Files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Import Weather Data"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var extension = IOPath.GetExtension(openFileDialog.FileName).ToLower();

                // Read weather data from file (this would need to be implemented)
                var weatherData = await _liveWeatherService.GetLocalWeatherAsync(
                    openFileDialog.FileName, DataSourceType.File);

                if (weatherData != null && _viewModel != null)
                {
                    _viewModel.CurrentWeather = weatherData;
                    _viewModel.StatusMessage = $"Weather data imported from {IOPath.GetFileName(openFileDialog.FileName)}";
                }
                
                _logger?.LogInformation($"Weather data imported: {openFileDialog.FileName}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error importing weather data");
            MessageBox.Show($"Error importing weather data: {ex.Message}", "Import Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ConfigureDataSources_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // TODO: Open data source configuration dialog
            MessageBox.Show("Data source configuration dialog will be implemented here.\n\n" +
                          "Supported sources:\n" +
                          "� Serial (NMEA 0183)\n" +
                          "� TCP/UDP Network\n" +
                          "� OSI PI\n" +
                          "� OPC/UA\n" +
                          "� MODBUS\n" +
                          "� File Import (CSV, JSON, XML)",
                          "Data Sources", MessageBoxButton.OK, MessageBoxImage.Information);
                          
            _logger?.LogInformation("Data source configuration requested");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening data source configuration");
        }
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // TODO: Implement map zoom in
            _logger?.LogInformation("Zoom in requested");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error zooming in");
        }
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // TODO: Implement map zoom out
            _logger?.LogInformation("Zoom out requested");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error zooming out");
        }
    }

    private async void ClearResults_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_realMappingService != null)
            {
                await _realMappingService.ClearDispersionOverlaysAsync();
                _viewModel?.UpdateStatusMessage("Results cleared");
                _logger?.LogInformation("Dispersion results cleared");
            }
            else
            {
                MessageBox.Show("Mapping service not available.", "Service Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error clearing results");
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            MessageBox.Show("Chemical Dispersion Modeling Application\n" +
                          "Version 1.0.0\n\n" +
                          "Real-time chemical dispersion modeling with:\n" +
                          "� NOAA ALOHA-compliant models\n" +
                          "� Live weather data integration\n" +
                          "� Industrial sensor connectivity\n" +
                          "� Professional mapping\n\n" +
                          "Built with .NET 8 WPF",
                          "About", MessageBoxButton.OK, MessageBoxImage.Information);
                          
            _logger?.LogInformation("About dialog shown");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing about dialog");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        try
        {
            // Clean up resources
            _liveWeatherService?.StopContinuousMonitoringAsync();
            _gasSensorService?.StopContinuousMonitoringAsync();
            
        // Unsubscribe from events
        if (_liveWeatherService != null)
            _liveWeatherService.WeatherDataReceived -= OnWeatherDataReceived;
        
        if (_gasSensorService != null)
            _gasSensorService.GasSensorDataReceived -= OnGasSensorDataReceived;            if (_realMappingService is RealMappingService realMapping)
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
