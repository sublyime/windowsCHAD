using System.Configuration;
using System.Data;
using System.Windows;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ChemicalDispersionModeling.Desktop.ViewModels;
using ChemicalDispersionModeling.Core.Services;
using ChemicalDispersionModeling.Core.Services.Implementation;
using ChemicalDispersionModeling.Data.Context;

namespace ChemicalDispersionModeling.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        Console.WriteLine("=== Application OnStartup ===");
        // Create host builder
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services, context.Configuration);
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });

        _host = hostBuilder.Build();

        // Add global exception handler
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var exception = e.ExceptionObject as Exception;
            MessageBox.Show($"Unhandled exception: {exception?.Message}\n\nStack trace:\n{exception?.StackTrace}", 
                "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (sender, e) =>
        {
            MessageBox.Show($"Dispatcher exception: {e.Exception.Message}\n\nStack trace:\n{e.Exception.StackTrace}", 
                "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };

        try
        {
            Console.WriteLine("Starting host...");
            await _host.StartAsync();
            Console.WriteLine("Host started successfully.");
            
            // Create and show main window
            Console.WriteLine("Getting MainWindow service...");
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            Console.WriteLine("Getting MainViewModel service...");
            var viewModel = _host.Services.GetRequiredService<MainViewModel>();
            Console.WriteLine("Setting DataContext...");
            mainWindow.DataContext = viewModel;
            Console.WriteLine("Showing main window...");
            mainWindow.Show();
            Console.WriteLine("MainWindow.Show() completed.");
            
            base.OnStartup(e);
            Console.WriteLine("OnStartup completed.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Application startup failed: {ex.Message}", "Startup Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Database configuration
        services.AddDbContext<ChemicalDispersionContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                "Host=localhost;Database=javadisp;Username=postgres;Password=ala1nna";
            options.UseNpgsql(connectionString);
        });

        // Core Services - Use mock implementations to avoid conflicts
        services.AddScoped<IWeatherService, WeatherService>();
        services.AddScoped<ITerrainService, TerrainService>();

        // Real Data Services
        services.AddScoped<ILiveWeatherService, Core.Services.Implementation.LiveWeatherService>();
        services.AddScoped<IGasSensorService, Core.Services.Implementation.GasSensorService>();
        services.AddScoped<IRealMappingService, Desktop.Services.RealMappingService>();

        // Dispersion Service
        services.AddScoped<IDispersionModelingService, DispersionModelingService>();

        // HTTP Client for weather services
        services.AddHttpClient<ILiveWeatherService, Core.Services.Implementation.LiveWeatherService>();

        // ViewModels
        services.AddTransient<MainViewModel>();

        // Windows
        services.AddTransient<MainWindow>();
    }
}

// Placeholder service implementations - these will be implemented in separate files
public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Core.Models.WeatherData?> GetCurrentWeatherAsync(double latitude, double longitude)
    {
        // Placeholder implementation - return mock data
        await Task.Delay(1000); // Simulate API call
        
        return new Core.Models.WeatherData
        {
            StationId = "MOCK_STATION",
            Latitude = latitude,
            Longitude = longitude,
            Timestamp = DateTime.UtcNow,
            Temperature = 20.0 + (new Random().NextDouble() - 0.5) * 10,
            Humidity = 60.0 + (new Random().NextDouble() - 0.5) * 20,
            Pressure = 1013.25,
            WindSpeed = 2.0 + new Random().NextDouble() * 8,
            WindDirection = new Random().NextDouble() * 360,
            StabilityClass = "D",
            Source = "Mock Weather Service"
        };
    }

    public Task<IEnumerable<Core.Models.WeatherData>> GetWeatherForecastAsync(double latitude, double longitude, DateTime startTime, DateTime endTime)
    {
        throw new NotImplementedException();
    }

    public Task<Core.Models.WeatherData?> GetNwsWeatherAsync(double latitude, double longitude)
    {
        throw new NotImplementedException();
    }

    public Task<Core.Models.WeatherData?> GetOpenMeteoWeatherAsync(double latitude, double longitude)
    {
        throw new NotImplementedException();
    }

    public string CalculateStabilityClass(double windSpeed, double cloudCover, bool isDaytime, double? solarRadiation = null)
    {
        // Simplified Pasquill-Gifford stability classification
        if (isDaytime)
        {
            if (windSpeed < 2) return cloudCover > 50 ? "D" : "A";
            if (windSpeed < 3) return cloudCover > 50 ? "D" : "B";
            if (windSpeed < 5) return cloudCover > 50 ? "D" : "C";
            return "D";
        }
        else
        {
            if (windSpeed < 2) return "F";
            if (windSpeed < 3) return cloudCover > 50 ? "E" : "F";
            return "D";
        }
    }
}

public class DispersionModelingService : IDispersionModelingService
{
    private readonly ILogger<DispersionModelingService> _logger;

    public DispersionModelingService(ILogger<DispersionModelingService> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<Core.Models.DispersionResult>> CalculateGaussianPlumeAsync(
        Core.Models.Release release, 
        IEnumerable<Core.Models.Receptor> receptors, 
        Core.Models.WeatherData weather)
    {
        await Task.Delay(500); // Simulate calculation time
        
        var results = new List<Core.Models.DispersionResult>();
        var releaseRate = CalculateEffectiveReleaseRate(release, release.Chemical, weather);
        
        foreach (var receptor in receptors)
        {
            var distance = CalculateDistance(release.Latitude, release.Longitude, receptor.Latitude, receptor.Longitude);
            var (sigmaY, sigmaZ) = CalculateDispersionCoefficients(weather.StabilityClass ?? "D", distance, release.ReleaseHeight);
            
            // Simplified Gaussian plume calculation
            var concentration = CalculateGaussianPlume(releaseRate, distance, sigmaY, sigmaZ, weather.WindSpeed, release.ReleaseHeight, receptor.Height);
            
            var result = new Core.Models.DispersionResult
            {
                CalculationTime = DateTime.UtcNow,
                Latitude = receptor.Latitude,
                Longitude = receptor.Longitude,
                Height = receptor.Height,
                Concentration = concentration,
                ConcentrationUnit = "mg/m³",
                DistanceFromSource = distance,
                DirectionFromSource = CalculateBearing(release.Latitude, release.Longitude, receptor.Latitude, receptor.Longitude),
                WindSpeed = weather.WindSpeed,
                WindDirection = weather.WindDirection,
                StabilityClass = weather.StabilityClass ?? "D",
                Temperature = weather.Temperature,
                RiskLevel = DetermineRiskLevel(concentration, release.Chemical),
                ModelUsed = "Gaussian Plume",
                ReleaseId = release.Id,
                ReceptorId = receptor.Id,
                Release = release,
                Receptor = receptor
            };
            
            results.Add(result);
        }
        
        return results;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula for distance calculation
        var R = 6371000; // Earth's radius in meters
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
    {
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var y = Math.Sin(dLon) * Math.Cos(lat2 * Math.PI / 180);
        var x = Math.Cos(lat1 * Math.PI / 180) * Math.Sin(lat2 * Math.PI / 180) -
                Math.Sin(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Cos(dLon);
        var bearing = Math.Atan2(y, x) * 180 / Math.PI;
        return (bearing + 360) % 360;
    }

    private double CalculateGaussianPlume(double releaseRate, double distance, double sigmaY, double sigmaZ, double windSpeed, double releaseHeight, double receptorHeight)
    {
        if (distance == 0 || windSpeed == 0) return 0;
        
        var heightTerm = Math.Exp(-0.5 * Math.Pow((receptorHeight - releaseHeight) / sigmaZ, 2)) +
                        Math.Exp(-0.5 * Math.Pow((receptorHeight + releaseHeight) / sigmaZ, 2));
        
        var concentration = (releaseRate / (2 * Math.PI * sigmaY * sigmaZ * windSpeed)) * heightTerm;
        
        return Math.Max(0, concentration * 1000); // Convert to mg/m³
    }

    public Task<IEnumerable<Core.Models.DispersionResult>> CalculateDispersionGridAsync(Core.Models.Release release, Core.Models.WeatherData weather, double gridSize = 100, double maxDistance = 10000)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Core.Models.DispersionResult>> CalculateTimeVaryingDispersionAsync(Core.Models.Release release, IEnumerable<Core.Models.Receptor> receptors, IEnumerable<Core.Models.WeatherData> weatherSequence)
    {
        throw new NotImplementedException();
    }

    public double CalculateEffectiveReleaseRate(Core.Models.Release release, Core.Models.Chemical chemical, Core.Models.WeatherData weather)
    {
        return release.ReleaseRate ?? (release.TotalMass ?? 0) / 3600; // kg/s
    }

    public double CalculatePlumeRise(Core.Models.Release release, Core.Models.Chemical chemical, Core.Models.WeatherData weather)
    {
        return 0; // Simplified - no plume rise
    }

    public (double SigmaY, double SigmaZ) CalculateDispersionCoefficients(string stabilityClass, double distance, double releaseHeight)
    {
        // Pasquill-Gifford dispersion coefficients (simplified)
        var x = distance / 1000.0; // Convert to km
        
        double sigmaY, sigmaZ;
        
        switch (stabilityClass.ToUpper())
        {
            case "A": // Very unstable
                sigmaY = 0.22 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                sigmaZ = 0.20 * x;
                break;
            case "B": // Moderately unstable
                sigmaY = 0.16 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                sigmaZ = 0.12 * x;
                break;
            case "C": // Slightly unstable
                sigmaY = 0.11 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                sigmaZ = 0.08 * x * Math.Pow(1 + 0.0002 * distance, -0.5);
                break;
            case "D": // Neutral
                sigmaY = 0.08 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                sigmaZ = 0.06 * x * Math.Pow(1 + 0.0015 * distance, -0.5);
                break;
            case "E": // Slightly stable
                sigmaY = 0.06 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                sigmaZ = 0.03 * x * Math.Pow(1 + 0.0003 * distance, -1);
                break;
            case "F": // Moderately stable
                sigmaY = 0.04 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                sigmaZ = 0.016 * x * Math.Pow(1 + 0.0003 * distance, -1);
                break;
            default:
                sigmaY = 0.08 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                sigmaZ = 0.06 * x * Math.Pow(1 + 0.0015 * distance, -0.5);
                break;
        }
        
        return (Math.Max(1, sigmaY * 1000), Math.Max(1, sigmaZ * 1000)); // Convert back to meters
    }

    public string DetermineRiskLevel(double concentration, Core.Models.Chemical chemical)
    {
        if (chemical.ToxicityThreshold == null) return "Unknown";
        
        var threshold = chemical.ToxicityThreshold.Value;
        var ratio = concentration / threshold;
        
        if (ratio < 0.1) return "Low";
        if (ratio < 0.5) return "Medium";
        if (ratio < 1.0) return "High";
        return "Critical";
    }
}

public class TerrainService : ITerrainService
{
    private readonly ILogger<TerrainService> _logger;

    public TerrainService(ILogger<TerrainService> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<Core.Models.TerrainData>> GetTerrainDataAsync(double minLat, double minLon, double maxLat, double maxLon)
    {
        throw new NotImplementedException();
    }

    public Task<double> GetElevationAsync(double latitude, double longitude)
    {
        return Task.FromResult(10.0); // Mock elevation
    }

    public Task<IEnumerable<Core.Models.TerrainData>> GetBuildingsAsync(double latitude, double longitude, double radiusMeters)
    {
        throw new NotImplementedException();
    }

    public Task ImportTerrainDataAsync(string filePath, string fileType)
    {
        throw new NotImplementedException();
    }

    public Task<double> CalculateSurfaceRoughnessAsync(double latitude, double longitude, double radiusMeters)
    {
        return Task.FromResult(0.3); // Mock surface roughness for urban area
    }

    public Task<bool> HasSignificantBuildingsAsync(double latitude, double longitude, double radiusMeters, double minimumHeight = 10.0)
    {
        return Task.FromResult(true); // Mock - assume urban area has buildings
    }
}

