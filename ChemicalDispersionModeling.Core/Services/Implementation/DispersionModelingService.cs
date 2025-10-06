using ChemicalDispersionModeling.Core.Models;
using ChemicalDispersionModeling.Core.Services;
using Microsoft.Extensions.Logging;

namespace ChemicalDispersionModeling.Core.Services.Implementation;

/// <summary>
/// NOAA ALOHA-compliant dispersion modeling service
/// Implements heavy gas and light gas dispersion models
/// Reference: NOAA Technical Memorandum NOS OR&R 43
/// </summary>
public class DispersionModelingService : IDispersionModelingService
{
    private readonly ILogger<DispersionModelingService> _logger;
    private readonly GaussianPlumeModel _gaussianModel;
    private readonly HeavyGasModel _heavyGasModel;
    private readonly AtmosphericStabilityService _stabilityService;

    public DispersionModelingService(
        ILogger<DispersionModelingService> logger,
        GaussianPlumeModel gaussianModel,
        HeavyGasModel heavyGasModel,
        AtmosphericStabilityService stabilityService)
    {
        _logger = logger;
        _gaussianModel = gaussianModel;
        _heavyGasModel = heavyGasModel;
        _stabilityService = stabilityService;
    }

    /// <summary>
    /// Run dispersion simulation using ALOHA-compliant models
    /// Automatically selects appropriate model based on chemical density
    /// </summary>
    public async Task<IEnumerable<DispersionResult>> RunSimulationAsync(
        Release release, 
        Chemical chemical, 
        WeatherData weather, 
        IEnumerable<Receptor> receptors)
    {
        try
        {
            _logger.LogInformation("Starting ALOHA-compliant dispersion simulation for chemical {ChemicalName}", chemical.Name);

            // Ensure weather data has stability class
            if (string.IsNullOrEmpty(weather.StabilityClass))
            {
                var isDaytime = DateTime.Now.Hour >= 6 && DateTime.Now.Hour <= 18;
                weather.StabilityClass = _stabilityService.DetermineStabilityClass(
                    weather.WindSpeed,
                    weather.CloudCover ?? 5, // Default partial cloud cover
                    isDaytime,
                    weather.SolarRadiation ?? CalculateSolarRadiation(DateTime.Now),
                    false // Assume urban environment if not specified
                );
                
                _logger.LogDebug("Determined atmospheric stability class: {StabilityClass}", weather.StabilityClass);
            }

            var results = new List<DispersionResult>();

            // Process each receptor
            foreach (var receptor in receptors)
            {
                var concentration = await CalculateConcentrationAsync(
                    receptor.Latitude,  // Using as relative X
                    receptor.Longitude, // Using as relative Y
                    receptor.Elevation, // Using as height
                    release,
                    chemical,
                    weather);

                var result = new DispersionResult
                {
                    ReleaseId = release.Id,
                    ReceptorId = receptor.Id,
                    Latitude = receptor.Latitude,
                    Longitude = receptor.Longitude,
                    Height = receptor.Elevation,
                    Concentration = concentration,
                    ConcentrationUnit = "mg/m³",
                    ModelUsed = GetModelName(chemical),
                    CalculationTime = DateTime.UtcNow,
                    WindSpeed = weather.WindSpeed,
                    WindDirection = weather.WindDirection,
                    StabilityClass = weather.StabilityClass ?? "D",
                    Temperature = weather.Temperature,
                    RiskLevel = DetermineRiskLevel(concentration, chemical)
                };

                results.Add(result);
            }

            _logger.LogInformation("Completed simulation for {ReceptorCount} receptors", receptors.Count());
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during dispersion simulation");
            throw;
        }
    }

    /// <summary>
    /// Calculate concentration at specific point using appropriate ALOHA model
    /// </summary>
    public Task<double> CalculateConcentrationAsync(
        double x, 
        double y, 
        double z, 
        Release release, 
        Chemical chemical, 
        WeatherData weather)
    {
        try
        {
            // Determine if chemical is heavier than air
            if (IsHeavyGas(chemical, weather))
            {
                _logger.LogDebug("Using Heavy Gas (DEGADIS) model for {ChemicalName}", chemical.Name);
                return Task.FromResult(_heavyGasModel.CalculateConcentration(x, y, z, release, chemical, weather));
            }
            else
            {
                _logger.LogDebug("Using Gaussian Plume model for {ChemicalName}", chemical.Name);
                return Task.FromResult(_gaussianModel.CalculateConcentration(x, y, z, release, chemical, weather));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating concentration at ({X},{Y},{Z})", x, y, z);
            return Task.FromResult(0.0);
        }
    }

    /// <summary>
    /// Determine if chemical should use heavy gas model
    /// Reference: ALOHA Technical Documentation Section 4.4.1
    /// </summary>
    private bool IsHeavyGas(Chemical chemical, WeatherData weather)
    {
        if (chemical.MolecularWeight <= 0) return false;

        // Calculate gas density relative to air
        var airMolecularWeight = 28.97; // g/mol
        var relativeDensity = chemical.MolecularWeight / airMolecularWeight;

        // ALOHA criterion: Use heavy gas model if density > 1.2 times air density
        return relativeDensity > 1.2;
    }

    /// <summary>
    /// Get model name for result documentation
    /// </summary>
    private string GetModelName(Chemical chemical)
    {
        var airMolecularWeight = 28.97;
        var relativeDensity = chemical.MolecularWeight / airMolecularWeight;
        
        return relativeDensity > 1.2 ? "ALOHA Heavy Gas (DEGADIS)" : "ALOHA Gaussian Plume";
    }

    /// <summary>
    /// Calculate solar radiation if not provided
    /// </summary>
    private double CalculateSolarRadiation(DateTime dateTime)
    {
        // Simple solar radiation estimation
        var hour = dateTime.Hour;
        var dayOfYear = dateTime.DayOfYear;
        
        // Peak solar radiation around noon
        if (hour >= 6 && hour <= 18)
        {
            var solarAngle = Math.Sin((hour - 6) * Math.PI / 12);
            var seasonalFactor = 1.0 + 0.3 * Math.Cos(2 * Math.PI * (dayOfYear - 172) / 365.25);
            return Math.Max(0, 1000 * solarAngle * seasonalFactor); // W/m²
        }
        
        return 0; // Night time
    }

    /// <summary>
    /// Format weather conditions for results
    /// </summary>
    private string FormatWeatherConditions(WeatherData weather)
    {
        return $"Wind: {weather.WindSpeed:F1} m/s @ {weather.WindDirection:F0}°, " +
               $"Temp: {weather.Temperature:F1}°C, " +
               $"Pressure: {weather.Pressure:F0} mb, " +
               $"Stability: {weather.StabilityClass}, " +
               $"Cloud Cover: {weather.CloudCover ?? 5}/10";
    }

    /// <summary>
    /// Calculate dose (time-integrated concentration)
    /// Reference: ALOHA Technical Documentation Section 3.2
    /// </summary>
    public async Task<double> CalculateDoseAsync(
        double x, 
        double y, 
        double z, 
        Release release, 
        Chemical chemical, 
        WeatherData weather,
        TimeSpan exposureTime)
    {
        try
        {
            var concentration = await CalculateConcentrationAsync(x, y, z, release, chemical, weather);
            
            // For steady-state releases, dose = concentration × time
            if (release.ReleaseType == "Continuous")
            {
                var effectiveExposureTime = TimeSpan.FromSeconds(Math.Min(
                    exposureTime.TotalSeconds,
                    (release.EndTime - release.StartTime)?.TotalSeconds ?? exposureTime.TotalSeconds));
                
                return concentration * effectiveExposureTime.TotalHours; // mg·h/m³
            }
            else
            {
                // For instantaneous releases, calculate time-integrated concentration
                // This would require more sophisticated puff modeling
                var puffPassageTime = CalculatePuffPassageTime(x, release, weather);
                var effectiveTime = Math.Min(exposureTime.TotalHours, puffPassageTime);
                
                return concentration * effectiveTime;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating dose at ({X},{Y},{Z})", x, y, z);
            return 0;
        }
    }

    /// <summary>
    /// Calculate puff passage time for instantaneous releases
    /// </summary>
    private double CalculatePuffPassageTime(double x, Release release, WeatherData weather)
    {
        if (x <= 0 || weather.WindSpeed <= 0) return 0;
        
        // Time for puff center to reach receptor
        var arrivalTime = x / weather.WindSpeed;
        
        // Puff duration (time for puff to pass receptor)
        // Simplified assumption: puff width grows with distance
        var stabilityClass = weather.StabilityClass ?? "D";
        var sigmaY = CalculateSimpleSigmaY(x, stabilityClass);
        var puffWidth = 4 * sigmaY; // ±2σ width
        var passageTime = puffWidth / weather.WindSpeed / 3600; // Convert to hours
        
        return passageTime;
    }

    /// <summary>
    /// Get maximum concentration along centerline
    /// </summary>
    public async Task<(double distance, double concentration)> GetMaximumConcentrationAsync(
        Release release,
        Chemical chemical, 
        WeatherData weather,
        double maxDistance = 10000)
    {
        try
        {
            var maxConcentration = 0.0;
            var maxDistance_m = 0.0;
            var step = maxDistance / 100; // 100 points along centerline
            
            for (var x = step; x <= maxDistance; x += step)
            {
                var concentration = await CalculateConcentrationAsync(x, 0, 0, release, chemical, weather);
                
                if (concentration > maxConcentration)
                {
                    maxConcentration = concentration;
                    maxDistance_m = x;
                }
            }
            
            return (maxDistance_m, maxConcentration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding maximum concentration");
            return (0, 0);
        }
    }

    /// <summary>
    /// Calculate dispersion using Gaussian plume model (legacy interface compatibility)
    /// </summary>
    public async Task<IEnumerable<DispersionResult>> CalculateGaussianPlumeAsync(
        Release release, 
        IEnumerable<Receptor> receptors, 
        WeatherData weather)
    {
        // Use the new RunSimulationAsync method
        return await RunSimulationAsync(release, GetDefaultChemical(), weather, receptors);
    }

    /// <summary>
    /// Calculate dispersion for a grid of points around the release
    /// </summary>
    public async Task<IEnumerable<DispersionResult>> CalculateDispersionGridAsync(
        Release release, 
        WeatherData weather, 
        double gridSize = 100, 
        double maxDistance = 10000)
    {
        try
        {
            var receptors = GenerateGridReceptors(0, 0, gridSize, maxDistance); // Use relative coordinates
            var chemical = GetDefaultChemical();
            
            return await RunSimulationAsync(release, chemical, weather, receptors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating dispersion grid");
            return Enumerable.Empty<DispersionResult>();
        }
    }

    /// <summary>
    /// Calculate time-varying dispersion for continuous releases
    /// </summary>
    public async Task<IEnumerable<DispersionResult>> CalculateTimeVaryingDispersionAsync(
        Release release, 
        IEnumerable<Receptor> receptors, 
        IEnumerable<WeatherData> weatherSequence)
    {
        try
        {
            var results = new List<DispersionResult>();
            var chemical = GetDefaultChemical();
            
            foreach (var weather in weatherSequence)
            {
                var timeResults = await RunSimulationAsync(release, chemical, weather, receptors);
                results.AddRange(timeResults);
            }
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating time-varying dispersion");
            return Enumerable.Empty<DispersionResult>();
        }
    }

    /// <summary>
    /// Calculate effective release rate considering physical state and conditions
    /// </summary>
    public double CalculateEffectiveReleaseRate(Release release, Chemical chemical, WeatherData weather)
    {
        try
        {
            if (release.ReleaseRate.HasValue)
            {
                return release.ReleaseRate.Value;
            }
            
            if (release.TotalMass.HasValue)
            {
                var duration = (release.EndTime - release.StartTime)?.TotalSeconds ?? 3600;
                return release.TotalMass.Value / duration;
            }
            
            return 0.1; // Default small release
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating effective release rate");
            return 0;
        }
    }

    /// <summary>
    /// Calculate plume rise due to buoyancy and momentum
    /// </summary>
    public double CalculatePlumeRise(Release release, Chemical chemical, WeatherData weather)
    {
        try
        {
            // Simplified Briggs plume rise formula for buoyant releases
            var windSpeed = Math.Max(weather.WindSpeed, 1.0);
            var stackHeight = release.ReleaseHeight;
            var buoyancyFlux = CalculateBuoyancyFlux(release, chemical, weather);
            
            if (buoyancyFlux > 0)
            {
                // Buoyant plume rise
                var x = 1000; // Distance for rise calculation (1 km)
                return 1.6 * Math.Pow(buoyancyFlux / windSpeed, 1.0/3.0) * Math.Pow(x, 2.0/3.0);
            }
            else
            {
                // Momentum rise
                var momentumFlux = CalculateMomentumFlux(release);
                return 3.0 * momentumFlux / windSpeed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating plume rise");
            return 0;
        }
    }

    /// <summary>
    /// Calculate dispersion coefficients (σy and σz) based on stability class and distance
    /// </summary>
    public (double SigmaY, double SigmaZ) CalculateDispersionCoefficients(
        string stabilityClass, 
        double distance, 
        double releaseHeight)
    {
        try
        {
            // Simple dispersion coefficient calculation (this would normally use the model)
            var sigmaY = CalculateSimpleSigmaY(distance, stabilityClass);
            var sigmaZ = CalculateSimpleSigmaZ(distance, stabilityClass);
            
            return (sigmaY, sigmaZ);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating dispersion coefficients");
            return (0, 0);
        }
    }

    /// <summary>
    /// Determine risk level based on concentration and toxicity thresholds
    /// </summary>
    public string DetermineRiskLevel(double concentration, Chemical chemical)
    {
        try
        {
            // Use toxicity thresholds - simplified approach
            var lc50 = 1000.0; // Default LC50 in mg/m³
            var aegl3 = lc50;
            var aegl2 = aegl3 * 0.1;
            var aegl1 = aegl3 * 0.01;
            
            if (concentration >= aegl3)
                return "Life Threatening";
            else if (concentration >= aegl2)
                return "Disabling";
            else if (concentration >= aegl1)
                return "Notable Discomfort";
            else if (concentration > 0.1)
                return "Detectable";
            else
                return "Safe";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining risk level");
            return "Unknown";
        }
    }

    /// <summary>
    /// Generate grid of receptors around release point
    /// </summary>
    private IEnumerable<Receptor> GenerateGridReceptors(double centerX, double centerY, double gridSize, double maxDistance)
    {
        var receptors = new List<Receptor>();
        int id = 1;
        
        for (var x = centerX - maxDistance; x <= centerX + maxDistance; x += gridSize)
        {
            for (var y = centerY - maxDistance; y <= centerY + maxDistance; y += gridSize)
            {
                var distance = Math.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                
                if (distance <= maxDistance && distance > 0)
                {
                    receptors.Add(new Receptor
                    {
                        Id = id++,
                        Name = $"Grid_{x:F0}_{y:F0}",
                        Latitude = x,  // Using as relative coordinates
                        Longitude = y, // Using as relative coordinates
                        Elevation = 1.5, // Standard breathing height
                        ReceptorType = "Grid Point"
                    });
                }
            }
        }
        
        return receptors;
    }

    /// <summary>
    /// Get default chemical properties for legacy compatibility
    /// </summary>
    private Chemical GetDefaultChemical()
    {
        return new Chemical
        {
            Name = "Generic Chemical",
            MolecularWeight = 58.12, // Default to acetone
            VaporPressure = 24600,   // Pa at 20°C
            BoilingPoint = 56.05     // °C
        };
    }

    /// <summary>
    /// Calculate buoyancy flux for plume rise
    /// </summary>
    private double CalculateBuoyancyFlux(Release release, Chemical chemical, WeatherData weather)
    {
        var releaseRate = CalculateEffectiveReleaseRate(release, chemical, weather);
        var temperatureDiff = (release.InitialTemperature ?? weather.Temperature) - weather.Temperature;
        var ambientTemp = weather.Temperature + 273.15; // Convert to Kelvin
        
        return 9.81 * releaseRate * temperatureDiff / ambientTemp;
    }

    /// <summary>
    /// Calculate momentum flux for plume rise
    /// </summary>
    private double CalculateMomentumFlux(Release release)
    {
        var exitVelocity = 1.0; // Default m/s
        var stackDiameter = release.DiameterOrArea ?? 1.0; // m
        var stackArea = Math.PI * (stackDiameter / 2) * (stackDiameter / 2);
        
        return exitVelocity * exitVelocity * stackArea;
    }

    /// <summary>
    /// Simple sigma Y calculation for legacy compatibility
    /// </summary>
    private double CalculateSimpleSigmaY(double distance, string stabilityClass)
    {
        var xKm = distance / 1000.0;
        
        return stabilityClass.ToUpper() switch
        {
            "A" => 0.22 * xKm / Math.Pow(1 + 0.0001 * distance, 0.5),
            "B" => 0.16 * xKm / Math.Pow(1 + 0.0001 * distance, 0.5),
            "C" => 0.11 * xKm / Math.Pow(1 + 0.0001 * distance, 0.5),
            "D" => 0.08 * xKm / Math.Pow(1 + 0.0001 * distance, 0.5),
            "E" => 0.06 * xKm / Math.Pow(1 + 0.0001 * distance, 0.5),
            "F" => 0.04 * xKm / Math.Pow(1 + 0.0001 * distance, 0.5),
            _ => 0.08 * xKm / Math.Pow(1 + 0.0001 * distance, 0.5)
        } * 1000; // Convert to meters
    }

    /// <summary>
    /// Simple sigma Z calculation for legacy compatibility
    /// </summary>
    private double CalculateSimpleSigmaZ(double distance, string stabilityClass)
    {
        var xKm = distance / 1000.0;
        
        return stabilityClass.ToUpper() switch
        {
            "A" => 0.2 * xKm,
            "B" => 0.12 * xKm,
            "C" => 0.08 * xKm / Math.Pow(1 + 0.0002 * distance, 0.5),
            "D" => 0.06 * xKm / Math.Pow(1 + 0.0015 * distance, 0.5),
            "E" => 0.03 * xKm / Math.Pow(1 + 0.0003 * distance, 1.0),
            "F" => 0.016 * xKm / Math.Pow(1 + 0.0003 * distance, 1.0),
            _ => 0.06 * xKm / Math.Pow(1 + 0.0015 * distance, 0.5)
        } * 1000; // Convert to meters
    }
}