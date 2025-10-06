using ChemicalDispersionModeling.Core.Models;
using Microsoft.Extensions.Logging;

namespace ChemicalDispersionModeling.Core.Services.Implementation;

/// <summary>
/// NOAA ALOHA-compliant atmospheric stability classification service
/// Based on NOAA Technical Memorandum NOS OR&R 43 Section 4.2
/// </summary>
public class AtmosphericStabilityService
{
    private readonly ILogger<AtmosphericStabilityService> _logger;

    public AtmosphericStabilityService(ILogger<AtmosphericStabilityService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Determine Pasquill-Gifford-Turner stability class using manual data entry method
    /// Reference: ALOHA Technical Documentation Section 4.2.1, Table 10
    /// </summary>
    public string DetermineStabilityClass(double windSpeed, double cloudCover, bool isDaytime, 
        double? solarInsolation = null, bool overWater = false)
    {
        // For plumes over water, assume stable conditions regardless of other factors
        if (overWater)
        {
            return "E"; // Stable conditions over water
        }

        // Completely overcast conditions (>50% cloud cover) = D regardless of time
        if (cloudCover > 50)
        {
            return "D";
        }

        if (isDaytime)
        {
            return DetermineDaytimeStability(windSpeed, solarInsolation ?? 0);
        }
        else
        {
            return DetermineNighttimeStability(windSpeed, cloudCover);
        }
    }

    /// <summary>
    /// Calculate solar insolation based on ALOHA formula
    /// Reference: ALOHA Technical Documentation Section 4.2.1
    /// </summary>
    public double CalculateSolarInsolation(double latitude, double longitude, DateTime dateTime, double cloudCover)
    {
        var solarAltitude = CalculateSolarAltitude(latitude, longitude, dateTime);
        
        if (solarAltitude <= 0.1)
        {
            return 0; // Night time or very low sun
        }

        // ALOHA formula for solar insolation
        var cloudinessFactor = 1.0 - 0.71 * (cloudCover / 10.0);
        var insolation = 1100 * Math.Sin(solarAltitude * Math.PI / 180.0) * cloudinessFactor;
        
        return Math.Max(0, insolation);
    }

    /// <summary>
    /// Calculate solar altitude angle
    /// Reference: ALOHA Technical Documentation Section 4.2.1
    /// </summary>
    private double CalculateSolarAltitude(double latitude, double longitude, DateTime dateTime)
    {
        var latRad = latitude * Math.PI / 180.0;
        var julianDay = dateTime.DayOfYear;
        
        // Solar declination
        var declination = 23.45 * Math.Sin((360.0 * (284 + julianDay) / 365.0) * Math.PI / 180.0) * Math.PI / 180.0;
        
        // Hour angle
        var hourAngle = 15.0 * (dateTime.Hour + dateTime.Minute / 60.0 - 12.0) * Math.PI / 180.0;
        
        // Solar altitude
        var sinAltitude = Math.Sin(latRad) * Math.Sin(declination) + 
                         Math.Cos(latRad) * Math.Cos(declination) * Math.Cos(hourAngle);
        
        return Math.Asin(sinAltitude) * 180.0 / Math.PI;
    }

    /// <summary>
    /// Determine daytime stability based on wind speed and solar insolation
    /// Reference: ALOHA Technical Documentation Table 10
    /// </summary>
    private string DetermineDaytimeStability(double windSpeed, double solarInsolation)
    {
        string insolationLevel;
        
        // Classify insolation level according to ALOHA documentation
        if (solarInsolation > 851)
            insolationLevel = "Strong";
        else if (solarInsolation > 526)
            insolationLevel = "Moderate";
        else if (solarInsolation > 176)
            insolationLevel = "Slight";
        else
            insolationLevel = "None";

        // Apply ALOHA Table 10 logic
        if (windSpeed < 2)
        {
            return insolationLevel switch
            {
                "Strong" => "A",
                "Moderate" => "A", // A-B, selecting most stable
                "Slight" => "B",
                _ => "D"
            };
        }
        else if (windSpeed < 3)
        {
            return insolationLevel switch
            {
                "Strong" => "A", // A-B, selecting most stable  
                "Moderate" => "B",
                "Slight" => "C",
                _ => "D"
            };
        }
        else if (windSpeed < 5)
        {
            return insolationLevel switch
            {
                "Strong" => "B",
                "Moderate" => "B", // B-C, selecting most stable
                "Slight" => "C",
                _ => "D"
            };
        }
        else if (windSpeed < 6)
        {
            return insolationLevel switch
            {
                "Strong" => "C",
                "Moderate" => "C", // C-D, selecting most stable
                "Slight" => "D",
                _ => "D"
            };
        }
        else // windSpeed >= 6
        {
            return insolationLevel switch
            {
                "Strong" => "C",
                "Moderate" => "D",
                "Slight" => "D",
                _ => "D"
            };
        }
    }

    /// <summary>
    /// Determine nighttime stability based on wind speed and cloud cover
    /// Reference: ALOHA Technical Documentation Table 10
    /// </summary>
    private string DetermineNighttimeStability(double windSpeed, double cloudCover)
    {
        if (windSpeed < 2)
        {
            return cloudCover > 50 ? "E" : "F";
        }
        else if (windSpeed < 3)
        {
            return cloudCover > 50 ? "E" : "F";
        }
        else if (windSpeed < 5)
        {
            return cloudCover > 50 ? "D" : "E";
        }
        else if (windSpeed < 6)
        {
            return "D";
        }
        else // windSpeed >= 6
        {
            return "D";
        }
    }

    /// <summary>
    /// Get stability class description for display purposes
    /// </summary>
    public string GetStabilityClassDescription(string stabilityClass)
    {
        return stabilityClass?.ToUpper() switch
        {
            "A" => "Extremely Unstable",
            "B" => "Moderately Unstable", 
            "C" => "Slightly Unstable",
            "D" => "Neutral",
            "E" => "Slightly Stable",
            "F" => "Moderately Stable",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Determine if conditions favor heavy gas behavior
    /// Reference: ALOHA Technical Documentation Section 4.4.1
    /// </summary>
    public bool ShouldUseHeavyGasModel(Chemical chemical, WeatherData weather, Release release)
    {
        // Calculate bulk density of release
        var airDensity = CalculateAirDensity(weather.Temperature, weather.Pressure);
        var chemicalDensity = chemical.Density;
        
        // For gas releases, consider molecular weight effect
        if (release.Scenario == "Gas")
        {
            var airMolWeight = 28.97; // g/mol for air
            var densityRatio = chemical.MolecularWeight / airMolWeight;
            
            // If chemical is significantly denser than air, consider heavy gas model
            return densityRatio > 1.2;
        }
        
        // For other scenarios, consider if aerosol/vapor mixture will be dense
        return chemicalDensity > airDensity * 1.1;
    }

    /// <summary>
    /// Calculate air density based on temperature and pressure
    /// </summary>
    private double CalculateAirDensity(double temperatureC, double pressureMb)
    {
        var temperatureK = temperatureC + 273.15;
        var pressurePa = pressureMb * 100; // Convert mb to Pa
        var R = 287.0; // Specific gas constant for dry air (J/kg·K)
        
        return pressurePa / (R * temperatureK); // kg/m³
    }
}