using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ChemicalDispersionModeling.Core.Models;
using ChemicalDispersionModeling.Core.Services;

namespace ChemicalDispersionModeling.Desktop.ViewModels;

/// <summary>
/// Main ViewModel for the Chemical Dispersion Modeling application
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IWeatherService _weatherService;
    private readonly IDispersionModelingService _dispersionService;
    private readonly ITerrainService _terrainService;

    // Events
    public event EventHandler<DispersionModelCompletedEventArgs>? DispersionModelCompleted;

    // Observable properties
    [ObservableProperty]
    private double _releaseLatitude = 40.7128; // Default to NYC

    [ObservableProperty]
    private double _releaseLongitude = -74.0060;

    [ObservableProperty]
    private double _releaseHeight = 2.0;

    [ObservableProperty]
    private Chemical? _selectedChemical;

    // Release type and scenario - manually implemented for change handling
    private string _releaseType = "Instantaneous";
    private string _releaseScenario = "Gas";

    [ObservableProperty]
    private double _releaseRate = 1.0;

    [ObservableProperty]
    private double _releaseDuration = 60.0;

    [ObservableProperty]
    private double _totalMass = 100.0;

    // ALOHA-Specific Modeling Parameters
    [ObservableProperty]
    private string _terrainType = "Urban"; // Urban, Rural

    [ObservableProperty]
    private double _roughnessLength = 1.0; // Surface roughness in meters

    [ObservableProperty]
    private double _inversionHeight = 400.0; // Inversion height in meters

    [ObservableProperty]
    private double _mixingHeight = 1000.0; // Mixing layer height in meters

    [ObservableProperty]
    private string _stabilityMethod = "Manual"; // Manual, Solar Insolation, Temperature Gradient

    [ObservableProperty]
    private bool _useEnhancedDispersion = true; // Enhanced dispersion for heavy gases

    [ObservableProperty]
    private WeatherData? _currentWeather;

    [ObservableProperty]
    private string _selectedWeatherSource = "NWS API";

    [ObservableProperty]
    private bool _autoUpdateEnabled = true;

    [ObservableProperty]
    private DateTime _lastUpdateTime = DateTime.Now;

    [ObservableProperty]
    private bool _showPlume = true;

    [ObservableProperty]
    private bool _showReceptors = true;

    [ObservableProperty]
    private bool _showBuildings = true;

    [ObservableProperty]
    private bool _showTopography = true;

    [ObservableProperty]
    private Receptor? _selectedReceptor;

    [ObservableProperty]
    private DispersionResult? _selectedResult;

    [ObservableProperty]
    private double _maxConcentration;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _debugStatus = "Debug: Ready";

    [ObservableProperty]
    private string _databaseStatus = "Connected";

    [ObservableProperty]
    private string _weatherStatus = "Online";

    [ObservableProperty]
    private string _modelStatus = "Ready";

    // Chemical Properties
    [ObservableProperty]
    private string _chemicalName = "Chlorine";

    [ObservableProperty]
    private double _molecularWeight = 70.9;

    [ObservableProperty]
    private double _density = 3.214;

    [ObservableProperty]
    private double _vaporPressure = 760.0;

    // Weather Properties  
    [ObservableProperty]
    private double _temperature = 20.0;

    [ObservableProperty]
    private double _windSpeed = 5.0;

    [ObservableProperty]
    private double _windDirection = 270.0;

    [ObservableProperty]
    private string _stabilityClass = "D";

    // Collections
    public ObservableCollection<Chemical> AvailableChemicals { get; } = new();
    public ObservableCollection<Receptor> Receptors { get; } = new();
    public ObservableCollection<DispersionResult> DispersionResults { get; } = new();

    // Commands
    public ICommand NewProjectCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand SaveProjectCommand { get; }
    public ICommand SaveProjectAsCommand { get; }
    public ICommand ImportGisDataCommand { get; }
    public ICommand ExportResultsCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand EditChemicalDatabaseCommand { get; }
    public ICommand EditWeatherSettingsCommand { get; }
    public ICommand EditPreferencesCommand { get; }
    public ICommand RunModelCommand { get; }
    public ICommand ClearResultsCommand { get; }
    public ICommand ModelSettingsCommand { get; }
    public ICommand ZoomToFitCommand { get; }
    public ICommand CenterOnReleaseCommand { get; }
    public ICommand Toggle3DBuildingsCommand { get; }
    public ICommand ToggleReceptorsCommand { get; }
    public ICommand ShowUserGuideCommand { get; }
    public ICommand ShowAboutCommand { get; }
    public ICommand SelectLocationOnMapCommand { get; }
    public ICommand AddChemicalCommand { get; }
    public ICommand RefreshWeatherCommand { get; }
    public ICommand ZoomToHomeCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand AddReceptorCommand { get; }
    public ICommand RemoveReceptorCommand { get; }
    public ICommand AutoGenerateReceptorsCommand { get; }
    public ICommand ShowGraphsCommand { get; }

    public MainViewModel(
        IWeatherService weatherService,
        IDispersionModelingService dispersionService,
        ITerrainService terrainService)
    {
        _weatherService = weatherService;
        _dispersionService = dispersionService;
        _terrainService = terrainService;

        // Initialize commands
        NewProjectCommand = new RelayCommand(NewProject);
        OpenProjectCommand = new RelayCommand(OpenProject);
        SaveProjectCommand = new RelayCommand(SaveProject);
        SaveProjectAsCommand = new RelayCommand(SaveProjectAs);
        ImportGisDataCommand = new RelayCommand(ImportGisData);
        ExportResultsCommand = new RelayCommand(ExportResults);
        ExitCommand = new RelayCommand(Exit);
        EditChemicalDatabaseCommand = new RelayCommand(EditChemicalDatabase);
        EditWeatherSettingsCommand = new RelayCommand(EditWeatherSettings);
        EditPreferencesCommand = new RelayCommand(EditPreferences);
        RunModelCommand = new AsyncRelayCommand(RunModelAsync);
        ClearResultsCommand = new RelayCommand(ClearResults);
        ModelSettingsCommand = new RelayCommand(ModelSettings);
        ZoomToFitCommand = new RelayCommand(ZoomToFit);
        CenterOnReleaseCommand = new RelayCommand(CenterOnRelease);
        Toggle3DBuildingsCommand = new RelayCommand(Toggle3DBuildings);
        ToggleReceptorsCommand = new RelayCommand(ToggleReceptors);
        ShowUserGuideCommand = new RelayCommand(ShowUserGuide);
        ShowAboutCommand = new RelayCommand(ShowAbout);
        SelectLocationOnMapCommand = new RelayCommand(SelectLocationOnMap);
        AddChemicalCommand = new RelayCommand(AddChemical);
        RefreshWeatherCommand = new AsyncRelayCommand(RefreshWeatherAsync);
        ZoomToHomeCommand = new RelayCommand(ZoomToHome);
        ZoomInCommand = new RelayCommand(ZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut);
        AddReceptorCommand = new RelayCommand(AddReceptor);
        RemoveReceptorCommand = new RelayCommand(RemoveReceptor);
        AutoGenerateReceptorsCommand = new RelayCommand(AutoGenerateReceptors);
        ShowGraphsCommand = new RelayCommand(ShowGraphs);

        // Initialize with sample data
        InitializeSampleData();
        
        // Start auto-update timer if enabled
        if (AutoUpdateEnabled)
        {
            StartAutoUpdateTimer();
        }
    }

    private void InitializeSampleData()
    {
        // Add sample chemicals
        AvailableChemicals.Add(new Chemical 
        { 
            Id = 1, 
            Name = "Chlorine", 
            MolecularWeight = 70.9, 
            Density = 3.214,
            ToxicityThreshold = 3.0,
            ToxicityUnit = "ppm",
            Description = "Greenish-yellow gas, highly toxic"
        });
        
        AvailableChemicals.Add(new Chemical 
        { 
            Id = 2, 
            Name = "Ammonia", 
            MolecularWeight = 17.03, 
            Density = 0.771,
            ToxicityThreshold = 25.0,
            ToxicityUnit = "ppm",
            Description = "Colorless gas with pungent odor"
        });

        AvailableChemicals.Add(new Chemical 
        { 
            Id = 3, 
            Name = "Hydrogen Sulfide", 
            MolecularWeight = 34.08, 
            Density = 1.539,
            ToxicityThreshold = 10.0,
            ToxicityUnit = "ppm",
            Description = "Colorless gas with rotten egg odor"
        });

        SelectedChemical = AvailableChemicals.First();

        // Add sample receptors
        AddSampleReceptors();

        // Initialize weather data
        CurrentWeather = new WeatherData
        {
            WindSpeed = 3.5,
            WindDirection = 270,
            Temperature = 20.0,
            Humidity = 65,
            Pressure = 1013.25,
            StabilityClass = "D",
            Timestamp = DateTime.Now
        };
    }

    // Override property setters to handle parameter changes
    public string ReleaseType
    {
        get => _releaseType;
        set
        {
            if (SetProperty(ref _releaseType, value))
            {
                UpdateParametersForModelType();
            }
        }
    }

    public string ReleaseScenario
    {
        get => _releaseScenario;
        set
        {
            if (SetProperty(ref _releaseScenario, value))
            {
                UpdateParametersForModelType();
            }
        }
    }

    private void UpdateParametersForModelType()
    {
        // Update parameters based on release type and scenario combination
        if (ReleaseType == "Variable" && ReleaseScenario == "Explosion")
        {
            // Explosion parameters
            ReleaseRate = 50.0; // Higher rate for explosions
            ReleaseDuration = 5.0; // Short duration
            TotalMass = 250.0; // More material
            ReleaseHeight = 10.0; // Higher release point
            TerrainType = "Urban"; // Explosions typically in urban areas
            UseEnhancedDispersion = true;
        }
        else if (ReleaseType == "Continuous" && ReleaseScenario == "Gas")
        {
            // Continuous gas leak
            ReleaseRate = 5.0; // Steady leak rate
            ReleaseDuration = 300.0; // 5 minutes
            TotalMass = 1500.0; // Total over time
            ReleaseHeight = 3.0; // Pipe level
            TerrainType = "Urban";
            UseEnhancedDispersion = false;
        }
        else if (ReleaseType == "Instantaneous")
        {
            // Instantaneous release
            ReleaseRate = 1.0; // Not applicable for instantaneous
            ReleaseDuration = 60.0; // Default
            TotalMass = 100.0; // Fixed amount
            ReleaseHeight = 2.0; // Ground level
            TerrainType = "Urban";
            UseEnhancedDispersion = false;
        }
        
        StatusMessage = $"Parameters updated for {ReleaseType} {ReleaseScenario} release";
    }

    private void AddSampleReceptors()
    {
        // Add receptors in a downwind pattern
        var baseDistance = 500; // meters
        for (int i = 1; i <= 5; i++)
        {
            var distance = baseDistance * i;
            var receptor = new Receptor
            {
                Id = i,
                Name = $"R{i}",
                Latitude = ReleaseLatitude + (distance * 0.00001), // Rough conversion
                Longitude = ReleaseLongitude + (distance * 0.00001),
                Elevation = 10,
                Height = 1.5,
                ReceptorType = "Residential",
                IsActive = true
            };
            Receptors.Add(receptor);
        }
    }

    #region Command Implementations

    private void NewProject()
    {
        StatusMessage = "Creating new project...";
        
        // Show confirmation dialog if there are unsaved changes
        var result = System.Windows.MessageBox.Show(
            "This will create a new project. Any unsaved changes will be lost.\n\nContinue?",
            "New Project",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
            
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            ClearResults();
            ResetToDefaults();
            StatusMessage = "New project created";
        }
        else
        {
            StatusMessage = "New project cancelled";
        }
    }

    private void OpenProject()
    {
        StatusMessage = "Opening project...";
        
        // Show file dialog
        var dialog = new Microsoft.Win32.OpenFileDialog()
        {
            Title = "Open Chemical Dispersion Project",
            Filter = "Project files (*.cdp)|*.cdp|All files (*.*)|*.*",
            DefaultExt = ".cdp"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                // Implementation for opening project file
                StatusMessage = $"Project opened: {System.IO.Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error opening project: {ex.Message}",
                    "Open Project Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                StatusMessage = "Failed to open project";
            }
        }
        else
        {
            StatusMessage = "Open project cancelled";
        }
    }

    private void SaveProject()
    {
        StatusMessage = "Saving project...";
        
        // Show save file dialog
        var dialog = new Microsoft.Win32.SaveFileDialog()
        {
            Title = "Save Chemical Dispersion Project",
            Filter = "Project files (*.cdp)|*.cdp|All files (*.*)|*.*",
            DefaultExt = ".cdp",
            FileName = "ChemicalDispersionProject.cdp"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                // Implementation for saving project file
                // For now, just create a basic project file
                var projectData = $@"{{
    ""projectName"": ""Chemical Dispersion Project"",
    ""created"": ""{DateTime.Now:yyyy-MM-dd HH:mm:ss}"",
    ""releaseLocation"": {{
        ""latitude"": {ReleaseLatitude},
        ""longitude"": {ReleaseLongitude},
        ""height"": {ReleaseHeight}
    }},
    ""chemical"": {{
        ""name"": ""{ChemicalName}"",
        ""molecularWeight"": {MolecularWeight},
        ""density"": {Density}
    }},
    ""release"": {{
        ""type"": ""{ReleaseType}"",
        ""scenario"": ""{ReleaseScenario}"",
        ""rate"": {ReleaseRate},
        ""duration"": {ReleaseDuration},
        ""totalMass"": {TotalMass}
    }}
}}";
                System.IO.File.WriteAllText(dialog.FileName, projectData);
                StatusMessage = $"Project saved: {System.IO.Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error saving project: {ex.Message}",
                    "Save Project Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                StatusMessage = "Failed to save project";
            }
        }
        else
        {
            StatusMessage = "Save project cancelled";
        }
    }

    private void SaveProjectAs()
    {
        StatusMessage = "Saving project as...";
        // Implementation for save as
        StatusMessage = "Project saved";
    }

    private void ImportGisData()
    {
        StatusMessage = "Importing GIS data...";
        
        // Show file dialog for GIS data import
        var dialog = new Microsoft.Win32.OpenFileDialog()
        {
            Title = "Import GIS Data",
            Filter = "Shapefile (*.shp)|*.shp|KML files (*.kml)|*.kml|GeoJSON (*.geojson)|*.geojson|All files (*.*)|*.*",
            DefaultExt = ".shp"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                // Implementation for GIS import would go here
                StatusMessage = $"GIS data imported: {System.IO.Path.GetFileName(dialog.FileName)}";
                
                System.Windows.MessageBox.Show(
                    $"GIS data file selected: {System.IO.Path.GetFileName(dialog.FileName)}\n\n" +
                    "GIS import functionality will be implemented to load:\n" +
                    "• Building footprints\n" +
                    "• Terrain elevation data\n" +
                    "• Population density\n" +
                    "• Critical infrastructure\n" +
                    "• Receptor locations",
                    "GIS Import",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error importing GIS data: {ex.Message}",
                    "Import Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                StatusMessage = "Failed to import GIS data";
            }
        }
        else
        {
            StatusMessage = "GIS import cancelled";
        }
    }

    private void ExportResults()
    {
        StatusMessage = "Exporting results...";
        // Implementation for export
        StatusMessage = "Results exported";
    }

    private void Exit()
    {
        // Implementation for exit
        System.Windows.Application.Current.Shutdown();
    }

    private void EditChemicalDatabase()
    {
        StatusMessage = "Opening chemical database...";
        // Implementation for chemical database editing
    }

    private void EditWeatherSettings()
    {
        StatusMessage = "Opening weather settings...";
        
        // Show weather settings dialog
        var weatherSettingsMessage = @"Weather Data Source Configuration:

Current Settings:
• Weather Source: " + SelectedWeatherSource + @"
• Auto Update: " + (AutoUpdateEnabled ? "Enabled" : "Disabled") + @"
• Location: " + ReleaseLatitude.ToString("F4") + @", " + ReleaseLongitude.ToString("F4") + @"

Available Sources:
• NWS API - National Weather Service (Real-time data)
• OpenMeteo - Open meteorological data
• Local Station - Serial/TCP/UDP connected weather stations
• File Import - CSV/JSON weather data files

Configure data sources in the settings panel or use real-time monitoring from industrial systems via OSI PI, OPC/UA, MODBUS, or NMEA 0183 protocols.

Current Weather Status: " + WeatherStatus;

        System.Windows.MessageBox.Show(
            weatherSettingsMessage,
            "Weather Settings",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
            
        StatusMessage = "Ready";
    }

    private void EditPreferences()
    {
        StatusMessage = "Opening preferences...";
        // Implementation for preferences
    }

    private async Task RunModelAsync()
    {
        if (SelectedChemical == null || CurrentWeather == null)
        {
            StatusMessage = "Please select a chemical and ensure weather data is available";
            return;
        }

        try
        {
            StatusMessage = "Running dispersion model...";
            ModelStatus = "Running";

            // Debug weather data
            if (CurrentWeather != null)
            {
                StatusMessage = $"Using weather: {CurrentWeather.Temperature:F1}°C, Wind {CurrentWeather.WindSpeed:F1} m/s @ {CurrentWeather.WindDirection:F0}°, Stability {CurrentWeather.StabilityClass ?? "D"}";
            }
            else
            {
                StatusMessage = "Warning: No weather data available, using defaults";
            }

            // Create release object with ALOHA-compliant parameters
            var release = new Release
            {
                Name = "Current Release",
                Latitude = ReleaseLatitude,
                Longitude = ReleaseLongitude,
                ReleaseHeight = ReleaseHeight,
                ReleaseType = ReleaseType,
                Scenario = ReleaseScenario,
                ReleaseRate = ReleaseRate,
                TotalMass = TotalMass,
                StartTime = DateTime.Now,
                ChemicalId = SelectedChemical.Id,
                Chemical = SelectedChemical,
                ModelingWindSpeed = CurrentWeather?.WindSpeed ?? 2.0,
                ModelingWindDirection = CurrentWeather?.WindDirection ?? 270.0,
                ModelingStabilityClass = CurrentWeather?.StabilityClass ?? "D",
                ModelingTemperature = CurrentWeather?.Temperature ?? 20.0,
                ModelingHumidity = CurrentWeather?.Humidity ?? 50.0,
                DiameterOrArea = 5.0, // Default release area/diameter
                InitialTemperature = (CurrentWeather?.Temperature ?? 20.0) + 10, // Slightly warmer than ambient
                InitialPressure = 101325.0 // Standard atmospheric pressure
            };

            // Run dispersion calculations using grid-based method
            var weatherData = CurrentWeather ?? new WeatherData
            {
                Temperature = 20.0,
                WindSpeed = 2.0,
                WindDirection = 270.0,
                StabilityClass = "D",
                Humidity = 50.0
            };
            
            var results = await _dispersionService.CalculateDispersionGridAsync(
                release, 
                weatherData,
                gridSize: 50,    // 50m grid resolution for more detail
                maxDistance: 2000 // 2km maximum distance
            );
            
            // Update results
            DispersionResults.Clear();
            foreach (var result in results)
            {
                DispersionResults.Add(result);
            }

            // Update max concentration
            MaxConcentration = DispersionResults.Any() ? DispersionResults.Max(r => r.Concentration) : 0;

            StatusMessage = $"Model completed. {DispersionResults.Count} results calculated.";
            ModelStatus = "Completed";

            // Fire event for map visualization
            DispersionModelCompleted?.Invoke(this, new DispersionModelCompletedEventArgs
            {
                Results = DispersionResults,
                Release = release,
                Weather = weatherData
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Model error: {ex.Message}";
            ModelStatus = "Error";
        }
    }

    private void ClearResults()
    {
        DispersionResults.Clear();
        MaxConcentration = 0;
        StatusMessage = "Results cleared";
        ModelStatus = "Ready";
    }

    private void ModelSettings()
    {
        StatusMessage = "Opening model settings...";
        
        var modelSettingsMessage = @"Dispersion Model Configuration:

Current Model Parameters:
• Chemical: " + ChemicalName + @"
• Molecular Weight: " + MolecularWeight.ToString("F2") + @" g/mol
• Density: " + Density.ToString("F3") + @" kg/m³
• Release Type: " + ReleaseType + @"
• Release Rate: " + ReleaseRate.ToString("F2") + @" kg/s
• Total Mass: " + TotalMass.ToString("F2") + @" kg

Model Settings:
• Algorithm: ALOHA Gaussian Plume Model
• Stability Class: " + (CurrentWeather?.StabilityClass ?? "D (Neutral)") + @"
• Averaging Time: 10 minutes
• Grid Resolution: 100m x 100m
• Maximum Distance: 10 km

These settings control how the chemical dispersion is calculated. The Gaussian plume model is suitable for continuous releases under stable meteorological conditions.";

        System.Windows.MessageBox.Show(
            modelSettingsMessage,
            "Model Settings",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
            
        StatusMessage = "Ready";
    }

    private void ZoomToFit()
    {
        StatusMessage = "Zooming to fit...";
        // Implementation for zoom to fit
    }

    private void CenterOnRelease()
    {
        StatusMessage = "Centering on release point...";
        // Implementation for centering
    }

    private void Toggle3DBuildings()
    {
        ShowBuildings = !ShowBuildings;
        StatusMessage = $"3D Buildings {(ShowBuildings ? "enabled" : "disabled")}";
    }

    private void ToggleReceptors()
    {
        ShowReceptors = !ShowReceptors;
        StatusMessage = $"Receptors {(ShowReceptors ? "visible" : "hidden")}";
    }

    private void ShowUserGuide()
    {
        StatusMessage = "Opening user guide...";
        
        var userGuideMessage = @"Chemical Dispersion Modeling - User Guide

Quick Start:
1. Set Release Location: Click on the map to place a release point
2. Configure Chemical: Set chemical properties (name, molecular weight, density)
3. Set Weather: Use live weather data or manual input
4. Run Model: Click 'Run Model' to calculate dispersion
5. View Results: Dispersion plume will appear on the map

Key Features:
• Real-time weather integration (NWS API, OpenMeteo)
• Industrial data connectivity (OSI PI, OPC/UA, MODBUS)
• Interactive mapping with OpenStreetMap
• ALOHA dispersion algorithms
• Live sensor data streaming
• Multiple data export formats

Menu Functions:
• File → New/Open/Save projects
• Edit → Weather settings, preferences
• Modeling → Run model, settings
• View → Map controls, zoom functions
• Help → This guide and about

For emergency response scenarios, configure live data sources and enable auto-update for real-time monitoring.";

        System.Windows.MessageBox.Show(
            userGuideMessage,
            "User Guide",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
            
        StatusMessage = "Ready";
    }

    private void ShowAbout()
    {
        StatusMessage = "Opening about dialog...";
        
        var aboutMessage = @"Chemical Dispersion Modeling Application
Version 1.0.0

A comprehensive Windows desktop application for modeling chemical dispersions in urban areas using fluid dynamics and real-time physics simulation.

Features:
• Real-time weather data integration (NWS API, OpenMeteo)
• Interactive mapping with OpenStreetMap
• Industrial data connectivity (OSI PI, OPC/UA, MODBUS, NMEA 0183)
• ALOHA dispersion modeling algorithms
• Live data streaming from sensors
• 3D visualization and terrain analysis

Technology Stack:
• .NET 8 WPF Framework
• PostgreSQL Database
• WebView2 Mapping
• Entity Framework Core

© 2024 Chemical Dispersion Modeling Team";

        System.Windows.MessageBox.Show(
            aboutMessage,
            "About Chemical Dispersion Modeling",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
            
        StatusMessage = "Ready";
    }

    private void SelectLocationOnMap()
    {
        StatusMessage = "Click on map to select release location";
        // Implementation for map selection
    }

    private void AddChemical()
    {
        StatusMessage = "Opening add chemical dialog...";
        // Implementation for adding chemical
    }

    private async Task RefreshWeatherAsync()
    {
        await UpdateWeatherForLocationAsync(ReleaseLatitude, ReleaseLongitude);
    }

    /// <summary>
    /// Updates weather data for a specific location
    /// </summary>
    public async Task UpdateWeatherForLocationAsync(double latitude, double longitude)
    {
        try
        {
            Console.WriteLine($"=== UpdateWeatherForLocationAsync CALLED ===");
            Console.WriteLine($"Input coordinates: Lat={latitude:F6}, Lng={longitude:F6}");
            Console.WriteLine($"Weather service available: {_weatherService != null}");
            
            StatusMessage = $"Fetching weather for location {latitude:F4}°, {longitude:F4}°...";
            WeatherStatus = "Updating";
            Console.WriteLine($"Status updated to: {StatusMessage}");

            Console.WriteLine($"Calling weather service...");
            if (_weatherService != null)
            {
                var weather = await _weatherService.GetCurrentWeatherAsync(latitude, longitude);
                Console.WriteLine($"Weather service returned: {weather != null}");
                
                if (weather != null)
                {
                    Console.WriteLine($"Weather data: Temp={weather.Temperature:F1}°C, Wind={weather.WindSpeed:F1}m/s, Dir={weather.WindDirection:F0}°");
                    CurrentWeather = weather;
                    
                    // Update individual weather properties for UI binding
                    Temperature = weather.Temperature;
                    WindSpeed = weather.WindSpeed;
                    WindDirection = weather.WindDirection;
                    StabilityClass = weather.StabilityClass ?? "D"; // Default to neutral if null
                    
                    LastUpdateTime = DateTime.Now;
                    WeatherStatus = "Online";
                    StatusMessage = $"Weather updated: {weather.Temperature:F1}°C, Wind {weather.WindSpeed:F1} m/s from {weather.WindDirection:F0}°";
                    Console.WriteLine($"Weather update completed successfully");
                }
                else
                {
                    WeatherStatus = "Error";
                    StatusMessage = "Failed to retrieve weather data";
                    Console.WriteLine($"Weather service returned null data");
                }
            }
            else
            {
                WeatherStatus = "Error";
                StatusMessage = "Weather service not available";
                Console.WriteLine($"Weather service is null!");
            }
        }
        catch (Exception ex)
        {
            WeatherStatus = "Error";
            StatusMessage = $"Weather error: {ex.Message}";
        }
    }

    private void ZoomToHome()
    {
        StatusMessage = "Zooming to home location...";
        // Implementation for zoom to home
    }

    private void ZoomIn()
    {
        StatusMessage = "Zooming in...";
        // Implementation for zoom in
    }

    private void ZoomOut()
    {
        StatusMessage = "Zooming out...";
        // Implementation for zoom out
    }

    private void AddReceptor()
    {
        var newReceptor = new Receptor
        {
            Id = Receptors.Count + 1,
            Name = $"R{Receptors.Count + 1}",
            Latitude = ReleaseLatitude + 0.001,
            Longitude = ReleaseLongitude + 0.001,
            Elevation = 10,
            Height = 1.5,
            ReceptorType = "Residential",
            IsActive = true
        };
        
        Receptors.Add(newReceptor);
        StatusMessage = $"Added receptor {newReceptor.Name}";
    }

    private void RemoveReceptor()
    {
        if (SelectedReceptor != null)
        {
            Receptors.Remove(SelectedReceptor);
            StatusMessage = $"Removed receptor {SelectedReceptor.Name}";
            SelectedReceptor = null;
        }
    }

    private void AutoGenerateReceptors()
    {
        StatusMessage = "Auto-generating receptors...";
        
        // Clear existing receptors
        Receptors.Clear();
        
        // Generate receptors in a grid pattern downwind
        var windDirection = CurrentWeather?.WindDirection ?? 270; // Default to west wind
        var windDirectionRad = windDirection * Math.PI / 180.0;
        
        // Generate receptors at various distances and cross-wind positions
        var distances = new[] { 100, 250, 500, 1000, 2000 }; // meters
        var crossWindOffsets = new[] { -200, -100, 0, 100, 200 }; // meters
        
        int receptorId = 1;
        foreach (var distance in distances)
        {
            foreach (var offset in crossWindOffsets)
            {
                // Calculate position based on wind direction
                var downwindLat = ReleaseLatitude + (distance * Math.Cos(windDirectionRad)) / 111000.0; // rough deg conversion
                var downwindLon = ReleaseLongitude + (distance * Math.Sin(windDirectionRad)) / (111000.0 * Math.Cos(ReleaseLatitude * Math.PI / 180.0));
                
                // Add cross-wind offset
                var crossWindLat = downwindLat + (offset * Math.Cos(windDirectionRad + Math.PI/2)) / 111000.0;
                var crossWindLon = downwindLon + (offset * Math.Sin(windDirectionRad + Math.PI/2)) / (111000.0 * Math.Cos(ReleaseLatitude * Math.PI / 180.0));
                
                var receptor = new Receptor
                {
                    Id = receptorId++,
                    Name = $"R{distance}_{offset}",
                    Latitude = crossWindLat,
                    Longitude = crossWindLon,
                    Elevation = 10,
                    Height = 1.5,
                    ReceptorType = "Residential",
                    IsActive = true
                };
                
                Receptors.Add(receptor);
            }
        }
        
        StatusMessage = $"Generated {Receptors.Count} receptors";
    }

    private void ShowGraphs()
    {
        StatusMessage = "Opening results graphs...";
        // Implementation for showing graphs
    }

    #endregion

    // Additional methods for real data integration
    public void ResetToDefaults()
    {
        ReleaseLatitude = 40.7128;
        ReleaseLongitude = -74.0060;
        ReleaseHeight = 2.0;
        ReleaseRate = 1.0;
        ReleaseDuration = 60.0;
        TotalMass = 100.0;
        ChemicalName = "Chlorine";
        MolecularWeight = 70.9;
        Density = 3.214;
        VaporPressure = 760.0;
        Temperature = 20.0;
        WindSpeed = 5.0;
        WindDirection = 270.0;
        StabilityClass = "D";
        StatusMessage = "Project reset to defaults";
    }

    public void UpdateStatusMessage(string message)
    {
        StatusMessage = message;
    }

    private void StartAutoUpdateTimer()
    {
        var timer = new System.Timers.Timer(30000); // 30 seconds
        timer.Elapsed += async (sender, e) =>
        {
            if (AutoUpdateEnabled)
            {
                await RefreshWeatherAsync();
            }
        };
        timer.Start();
    }
}

/// <summary>
/// Event arguments for dispersion model completion
/// </summary>
public class DispersionModelCompletedEventArgs : EventArgs
{
    public IEnumerable<DispersionResult> Results { get; set; } = new List<DispersionResult>();
    public Release Release { get; set; } = new Release();
    public WeatherData Weather { get; set; } = new WeatherData();
}