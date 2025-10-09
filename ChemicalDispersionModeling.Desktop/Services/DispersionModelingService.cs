using ChemicalDispersionModeling.Core.Models;
using ChemicalDispersionModeling.Core.Services;
using Microsoft.Extensions.Logging;

namespace ChemicalDispersionModeling.Desktop.Services;

/// <summary>
/// Implementation of atmospheric dispersion modeling using Gaussian plume model
/// </summary>
public class DispersionModelingService : IDispersionModelingService
{
    private readonly ILogger<DispersionModelingService> _logger;

    public DispersionModelingService(ILogger<DispersionModelingService> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<DispersionResult>> CalculateGaussianPlumeAsync(Release release, IEnumerable<Receptor> receptors, WeatherData weather)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation($"Calculating Gaussian plume for {receptors.Count()} receptors");
                
                var results = new List<DispersionResult>();
                var effectiveReleaseRate = CalculateEffectiveReleaseRate(release, release.Chemical!, weather);
                var plumeRise = CalculatePlumeRise(release, release.Chemical!, weather);
                var effectiveHeight = release.ReleaseHeight + plumeRise;
                var stabilityClass = DetermineStabilityClass(weather);
                
                foreach (var receptor in receptors)
                {
                    var distance = CalculateDistance(release.Latitude, release.Longitude, receptor.Latitude, receptor.Longitude);
                    
                    if (distance < 1.0) distance = 1.0; // Minimum distance to avoid singularity
                    
                    var concentration = CalculateGaussianConcentration(
                        effectiveReleaseRate,
                        distance,
                        effectiveHeight,
                        weather.WindSpeed,
                        stabilityClass);
                    
                    var result = new DispersionResult
                    {
                        Latitude = receptor.Latitude,
                        Longitude = receptor.Longitude,
                        Concentration = concentration,
                        ConcentrationUnit = "mg/m³",
                        CalculationTime = DateTime.Now,
                        WindSpeed = weather.WindSpeed,
                        WindDirection = weather.WindDirection,
                        Temperature = weather.Temperature,
                        RiskLevel = DetermineRiskLevel(concentration, release.Chemical!),
                        ModelUsed = "Gaussian Plume",
                        ReleaseId = release.Id,
                        ReceptorId = receptor.Id
                    };
                    
                    results.Add(result);
                }
                
                _logger.LogInformation($"Calculated concentrations for {results.Count} receptors");
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating Gaussian plume");
                throw;
            }
        });
    }

    public async Task<IEnumerable<DispersionResult>> CalculateDispersionGridAsync(Release release, WeatherData weather, double gridSize = 100, double maxDistance = 10000)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation($"Calculating dispersion grid with {gridSize}m resolution, max distance {maxDistance}m");
                
                var results = new List<DispersionResult>();
                var effectiveReleaseRate = CalculateEffectiveReleaseRate(release, release.Chemical!, weather);
                var plumeRise = CalculatePlumeRise(release, release.Chemical!, weather);
                var effectiveHeight = release.ReleaseHeight + plumeRise;
                var stabilityClass = DetermineStabilityClass(weather);
                
                // Convert wind direction to radians for calculations
                var windDirectionRad = weather.WindDirection * Math.PI / 180.0;
                
                // Clear existing results and create wind-oriented plume
                results.Clear();
                
                // Generate plume shape based on wind direction
                for (double downwindDistance = 10; downwindDistance <= maxDistance; downwindDistance += gridSize)
                {
                    // Calculate crosswind standard deviation (sigma_y) based on distance and stability
                    var sigmaY = CalculateCrosswindDispersion(downwindDistance, stabilityClass);
                    var sigmaZ = CalculateVerticalDispersion(downwindDistance, stabilityClass);
                    
                    // Generate crosswind points for this downwind distance
                    var crosswindSpan = Math.Min(sigmaY * 6, maxDistance); // 6 sigma covers ~99.7% of plume
                    var crosswindPoints = (int)(crosswindSpan / gridSize);
                    
                    for (int j = -crosswindPoints; j <= crosswindPoints; j++)
                    {
                        var crosswindOffset = j * gridSize;
                        
                        // Transform coordinates based on wind direction
                        var offsetX = downwindDistance * Math.Sin(windDirectionRad) + crosswindOffset * Math.Cos(windDirectionRad);
                        var offsetY = downwindDistance * Math.Cos(windDirectionRad) - crosswindOffset * Math.Sin(windDirectionRad);
                        
                        // Calculate actual lat/lon from offset
                        var lat = release.Latitude + (offsetY / 111000.0);
                        var lon = release.Longitude + (offsetX / (111000.0 * Math.Cos(release.Latitude * Math.PI / 180.0)));
                        
                        // Calculate realistic Gaussian concentration with crosswind decay
                        var concentration = CalculateWindOrientedConcentration(
                            effectiveReleaseRate,
                            downwindDistance,
                            crosswindOffset,
                            effectiveHeight,
                            weather.WindSpeed,
                            sigmaY,
                            sigmaZ);
                        
                        if (concentration > 0.001) // Only add points with significant concentration
                        {
                            var result = new DispersionResult
                            {
                                Latitude = lat,
                                Longitude = lon,
                                Concentration = concentration,
                                ConcentrationUnit = "mg/m³",
                                CalculationTime = DateTime.Now,
                                WindSpeed = weather.WindSpeed,
                                WindDirection = weather.WindDirection,
                                Temperature = weather.Temperature,
                                RiskLevel = DetermineRiskLevel(concentration, release.Chemical!),
                                ModelUsed = "Gaussian Plume Grid",
                                ReleaseId = release.Id
                            };
                            
                            results.Add(result);
                        }
                    }
                }
                
                _logger.LogInformation($"Generated {results.Count} grid points with significant concentrations");
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating dispersion grid");
                throw;
            }
        });
    }

    public async Task<IEnumerable<DispersionResult>> CalculateTimeVaryingDispersionAsync(Release release, IEnumerable<Receptor> receptors, IEnumerable<WeatherData> weatherSequence)
    {
        var allResults = new List<DispersionResult>();
        
        foreach (var weather in weatherSequence)
        {
            var timeResults = await CalculateGaussianPlumeAsync(release, receptors, weather);
            allResults.AddRange(timeResults);
        }
        
        return allResults;
    }

    public double CalculateEffectiveReleaseRate(Release release, Chemical chemical, WeatherData weather)
    {
        // Simple conversion from release rate to effective emission rate
        // This would normally account for chemical properties, temperature, etc.
        var baseRate = release.ReleaseRate ?? 1.0; // kg/s, default to 1 kg/s
        
        // Convert to mg/s for concentration calculations
        return baseRate * 1000000; // mg/s
    }

    public double CalculatePlumeRise(Release release, Chemical chemical, WeatherData weather)
    {
        // Simplified plume rise calculation
        // This would normally use more complex formulas based on buoyancy and momentum
        var releaseTemp = release.InitialTemperature ?? weather.Temperature + 20; // Default to 20°C above ambient
        var deltaT = releaseTemp - weather.Temperature;
        var buoyancyRise = deltaT > 0 ? deltaT * 0.5 : 0; // Very simplified
        
        return Math.Max(0, buoyancyRise);
    }

    public (double SigmaY, double SigmaZ) CalculateDispersionCoefficients(string stabilityClass, double distance, double height)
    {
        // Pasquill-Gifford dispersion coefficients
        var x = distance / 1000.0; // Convert to km for P-G formulas
        
        double sigmaY, sigmaZ;
        
        switch (stabilityClass.ToUpper())
        {
            case "A": // Very unstable
                sigmaY = 213 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                sigmaZ = 213 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                break;
            case "B": // Moderately unstable
                sigmaY = 156 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                sigmaZ = 156 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                break;
            case "C": // Slightly unstable
                sigmaY = 104 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                sigmaZ = 104 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                break;
            case "D": // Neutral
                sigmaY = 68 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                sigmaZ = 68 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                break;
            case "E": // Slightly stable
                sigmaY = 50.5 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                sigmaZ = 50.5 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                break;
            case "F": // Moderately stable
                sigmaY = 34 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                sigmaZ = 34 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                break;
            default:
                sigmaY = 68 * x * Math.Pow(1 + 0.0001 * distance, -0.5); // Default to neutral
                sigmaZ = 68 * x * Math.Pow(1 + 0.0001 * distance, -0.5);
                break;
        }
        
        return (Math.Max(1.0, sigmaY), Math.Max(1.0, sigmaZ));
    }

    public string DetermineRiskLevel(double concentration, Chemical chemical)
    {
        var threshold = chemical.ToxicityThreshold;
        
        if (concentration > threshold) return "HIGH";
        if (concentration > threshold * 0.1) return "MEDIUM";
        if (concentration > threshold * 0.01) return "LOW";
        return "MINIMAL";
    }

    private double CalculateGaussianConcentration(double releaseRate, double distance, double height, double windSpeed, string stabilityClass)
    {
        if (windSpeed <= 0) windSpeed = 0.1; // Avoid division by zero
        
        var (sigmaY, sigmaZ) = CalculateDispersionCoefficients(stabilityClass, distance, height);
        
        // Gaussian plume equation
        var concentration = (releaseRate / (2 * Math.PI * windSpeed * sigmaY * sigmaZ)) 
                          * Math.Exp(-Math.Pow(height, 2) / (2 * Math.Pow(sigmaZ, 2)));
        
        return Math.Max(0, concentration);
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula for distance calculation
        const double R = 6371000; // Earth radius in meters
        
        var lat1Rad = lat1 * Math.PI / 180;
        var lat2Rad = lat2 * Math.PI / 180;
        var deltaLat = (lat2 - lat1) * Math.PI / 180;
        var deltaLon = (lon2 - lon1) * Math.PI / 180;
        
        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return R * c;
    }

    private string DetermineStabilityClass(WeatherData weather)
    {
        // Simplified stability class determination
        // Normally this would use solar radiation, cloud cover, etc.
        var windSpeed = weather.WindSpeed;
        
        if (windSpeed < 2) return "F"; // Very stable
        if (windSpeed < 3) return "E"; // Slightly stable
        if (windSpeed < 5) return "D"; // Neutral
        if (windSpeed < 6) return "C"; // Slightly unstable
        return "B"; // Moderately unstable
    }
    
    private double CalculateCrosswindDispersion(double distance, string stabilityClass)
    {
        // Pasquill-Gifford dispersion coefficients for sigma_y
        // Based on stability class and distance in meters
        double a, b;
        
        switch (stabilityClass.ToUpper())
        {
            case "A": // Very unstable
                a = 0.22; b = 0.0001;
                break;
            case "B": // Moderately unstable
                a = 0.16; b = 0.0001;
                break;
            case "C": // Slightly unstable
                a = 0.11; b = 0.0001;
                break;
            case "D": // Neutral (default)
                a = 0.08; b = 0.0001;
                break;
            case "E": // Slightly stable
                a = 0.06; b = 0.0001;
                break;
            case "F": // Moderately stable
                a = 0.04; b = 0.0001;
                break;
            default:
                a = 0.08; b = 0.0001; // Default to neutral
                break;
        }
        
        return a * distance * Math.Pow(1 + b * distance, -0.5);
    }
    
    private double CalculateVerticalDispersion(double distance, string stabilityClass)
    {
        // Pasquill-Gifford dispersion coefficients for sigma_z
        double a, b;
        
        switch (stabilityClass.ToUpper())
        {
            case "A": // Very unstable
                a = 0.20; b = 0.0;
                break;
            case "B": // Moderately unstable
                a = 0.12; b = 0.0;
                break;
            case "C": // Slightly unstable
                a = 0.08; b = 0.0002;
                break;
            case "D": // Neutral (default)
                a = 0.06; b = 0.0015;
                break;
            case "E": // Slightly stable
                a = 0.03; b = 0.0003;
                break;
            case "F": // Moderately stable
                a = 0.016; b = 0.0003;
                break;
            default:
                a = 0.06; b = 0.0015; // Default to neutral
                break;
        }
        
        return a * distance * Math.Pow(1 + b * distance, -0.5);
    }
    
    private double CalculateWindOrientedConcentration(
        double releaseRate, 
        double downwindDistance, 
        double crosswindOffset, 
        double effectiveHeight, 
        double windSpeed, 
        double sigmaY, 
        double sigmaZ)
    {
        if (windSpeed <= 0 || downwindDistance <= 0 || sigmaY <= 0 || sigmaZ <= 0)
            return 0;
        
        // Classic Gaussian plume equation with crosswind decay
        var crosswindTerm = Math.Exp(-0.5 * Math.Pow(crosswindOffset / sigmaY, 2));
        var verticalTerm = Math.Exp(-0.5 * Math.Pow(effectiveHeight / sigmaZ, 2));
        
        // Add ground reflection term
        var reflectionTerm = Math.Exp(-0.5 * Math.Pow((effectiveHeight) / sigmaZ, 2));
        
        var concentration = (releaseRate / (2 * Math.PI * windSpeed * sigmaY * sigmaZ)) *
                           crosswindTerm * (verticalTerm + reflectionTerm);
        
        return Math.Max(0, concentration * 1000000); // Convert to mg/m³
    }
}