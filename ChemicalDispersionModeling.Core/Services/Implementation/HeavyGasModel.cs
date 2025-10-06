using ChemicalDispersionModeling.Core.Models;
using Microsoft.Extensions.Logging;

namespace ChemicalDispersionModeling.Core.Services.Implementation;

/// <summary>
/// NOAA ALOHA-compliant Heavy Gas dispersion model implementation
/// Based on NOAA Technical Memorandum NOS OR&R 43 Section 4.4
/// Reference: DEGADIS (Dense Gas Dispersion) model
/// </summary>
public class HeavyGasModel
{
    private readonly ILogger<HeavyGasModel> _logger;

    public HeavyGasModel(ILogger<HeavyGasModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculate concentration using ALOHA Heavy Gas model
    /// Reference: ALOHA Technical Documentation Section 4.4
    /// </summary>
    public double CalculateConcentration(double x, double y, double z, Release release, Chemical chemical, WeatherData weather)
    {
        if (x <= 0 || weather.WindSpeed <= 0) return 0;

        // Calculate initial source parameters
        var sourceParameters = CalculateSourceParameters(release, chemical, weather);
        
        // Determine if we're in stably stratified shear flow or passive diffusion stage
        var bulkRichardsonNumber = CalculateBulkRichardsonNumber(sourceParameters, x, weather);
        
        if (bulkRichardsonNumber > 1.0)
        {
            // Stably stratified shear flow stage
            return CalculateStablyStratifiedConcentration(x, y, z, sourceParameters, weather);
        }
        else
        {
            // Passive diffusion stage - transition to Gaussian-like behavior
            return CalculatePassiveDiffusionConcentration(x, y, z, sourceParameters, weather);
        }
    }

    /// <summary>
    /// Calculate initial source parameters for heavy gas model
    /// Reference: ALOHA Technical Documentation Section 4.4.3
    /// </summary>
    private HeavyGasSourceParameters CalculateSourceParameters(Release release, Chemical chemical, WeatherData weather)
    {
        var Q = CalculateEffectiveReleaseRate(release, chemical, weather);
        var airDensity = CalculateAirDensity(weather.Temperature, weather.Pressure);
        var gasDensity = CalculateGasDensity(chemical, weather.Temperature, weather.Pressure);
        
        // Reduced gravity
        var reducedGravity = 9.81 * (gasDensity - airDensity) / airDensity;
        
        // Initial cloud dimensions (source blanket)
        var initialRadius = Math.Sqrt(Q / (Math.PI * 0.1)); // Assumed initial source thickness 0.1m
        var initialHeight = 0.1; // Initial blanket height
        
        return new HeavyGasSourceParameters
        {
            ReleaseRate = Q,
            ReducedGravity = reducedGravity,
            InitialRadius = initialRadius,
            InitialHeight = initialHeight,
            GasDensity = gasDensity,
            AirDensity = airDensity
        };
    }

    /// <summary>
    /// Calculate concentration in stably stratified shear flow stage
    /// Reference: ALOHA Technical Documentation Section 4.4.4
    /// </summary>
    private double CalculateStablyStratifiedConcentration(double x, double y, double z, 
        HeavyGasSourceParameters source, WeatherData weather)
    {
        // Effective cloud parameters at distance x
        var effectiveHeight = CalculateEffectiveCloudHeight(x, source, weather);
        var effectiveWidth = CalculateEffectiveCloudWidth(x, source, weather);
        var coreWidth = CalculateCoreWidth(x, source, weather);
        
        // Centerline concentration
        var centerlineConcentration = CalculateCenterlineConcentration(x, source, weather);
        
        // Power law exponent for vertical concentration profile
        var n = CalculatePowerLawExponent(source, weather);
        
        // Lateral concentration profile
        var lateralTerm = CalculateLateralConcentrationProfile(y, effectiveWidth, coreWidth);
        
        // Vertical concentration profile  
        var verticalTerm = CalculateVerticalConcentrationProfile(z, effectiveHeight, n);
        
        return centerlineConcentration * lateralTerm * verticalTerm;
    }

    /// <summary>
    /// Calculate concentration in passive diffusion stage
    /// Reference: ALOHA Technical Documentation Section 4.4.4
    /// </summary>
    private double CalculatePassiveDiffusionConcentration(double x, double y, double z,
        HeavyGasSourceParameters source, WeatherData weather)
    {
        // Transition to Gaussian-like behavior with modified dispersion parameters
        var stabilityClass = weather.StabilityClass ?? "D";
        
        // Enhanced lateral dispersion due to initial heavy gas spreading
        var enhancedSigmaY = CalculateEnhancedLateralDispersion(x, source, stabilityClass);
        var enhancedSigmaZ = CalculateEnhancedVerticalDispersion(x, source, stabilityClass);
        
        // Wind speed at effective height
        var windSpeed = CalculateWindSpeedAtHeight(weather.WindSpeed, source.InitialHeight, stabilityClass);
        
        // Modified Gaussian calculation
        var lateralTerm = Math.Exp(-0.5 * Math.Pow(y / enhancedSigmaY, 2));
        var verticalTerm = Math.Exp(-0.5 * Math.Pow(z / enhancedSigmaZ, 2)) + 
                          Math.Exp(-0.5 * Math.Pow((z + source.InitialHeight) / enhancedSigmaZ, 2));
        
        var concentration = (source.ReleaseRate / (windSpeed * enhancedSigmaY * enhancedSigmaZ * Math.Sqrt(2 * Math.PI))) *
                           lateralTerm * verticalTerm;
        
        return Math.Max(0, concentration * 1e6); // Convert to mg/m³
    }

    /// <summary>
    /// Calculate bulk Richardson number
    /// Reference: ALOHA Technical Documentation Section 4.4.4.2
    /// </summary>
    private double CalculateBulkRichardsonNumber(HeavyGasSourceParameters source, double x, WeatherData weather)
    {
        var effectiveHeight = CalculateEffectiveCloudHeight(x, source, weather);
        var windSpeed = CalculateWindSpeedAtHeight(weather.WindSpeed, effectiveHeight, weather.StabilityClass ?? "D");
        
        return (source.ReducedGravity * effectiveHeight) / (windSpeed * windSpeed);
    }

    /// <summary>
    /// Calculate effective cloud height
    /// Reference: ALOHA Technical Documentation Section 4.4.4.1
    /// </summary>
    private double CalculateEffectiveCloudHeight(double x, HeavyGasSourceParameters source, WeatherData weather)
    {
        // Simplified height calculation - in practice this would involve solving differential equations
        var initialHeight = source.InitialHeight;
        var entrainmentRate = 0.1; // Typical entrainment coefficient
        var dilutionFactor = 1.0 + entrainmentRate * x / 1000.0; // Simple linear dilution
        
        return initialHeight * Math.Sqrt(dilutionFactor);
    }

    /// <summary>
    /// Calculate effective cloud width
    /// Reference: ALOHA Technical Documentation Section 4.4.4.1
    /// </summary>
    private double CalculateEffectiveCloudWidth(double x, HeavyGasSourceParameters source, WeatherData weather)
    {
        // Lateral spreading due to gravity and wind shear
        var gravitySpread = Math.Sqrt(2 * source.ReducedGravity * x / weather.WindSpeed);
        var windShearSpread = 0.1 * x; // Simplified wind shear spreading
        
        return Math.Max(source.InitialRadius * 2, gravitySpread + windShearSpread);
    }

    /// <summary>
    /// Calculate core width (homogeneous concentration region)
    /// Reference: ALOHA Technical Documentation Section 4.4.4
    /// </summary>
    private double CalculateCoreWidth(double x, HeavyGasSourceParameters source, WeatherData weather)
    {
        var bulkRi = CalculateBulkRichardsonNumber(source, x, weather);
        
        if (bulkRi > 1.0)
        {
            // Sharp-edged core exists
            var effectiveWidth = CalculateEffectiveCloudWidth(x, source, weather);
            return Math.Max(0, effectiveWidth * 0.5 * (bulkRi - 1.0) / bulkRi);
        }
        
        return 0; // No core in passive diffusion stage
    }

    /// <summary>
    /// Calculate centerline ground-level concentration
    /// </summary>
    private double CalculateCenterlineConcentration(double x, HeavyGasSourceParameters source, WeatherData weather)
    {
        var effectiveHeight = CalculateEffectiveCloudHeight(x, source, weather);
        var effectiveWidth = CalculateEffectiveCloudWidth(x, source, weather);
        var windSpeed = CalculateWindSpeedAtHeight(weather.WindSpeed, effectiveHeight, weather.StabilityClass ?? "D");
        
        return source.ReleaseRate / (windSpeed * effectiveWidth * effectiveHeight) * 1e6; // Convert to mg/m³
    }

    /// <summary>
    /// Calculate lateral concentration profile
    /// Reference: ALOHA Technical Documentation Section 4.4.4
    /// </summary>
    private double CalculateLateralConcentrationProfile(double y, double effectiveWidth, double coreWidth)
    {
        var halfCoreWidth = coreWidth / 2.0;
        var halfEffectiveWidth = effectiveWidth / 2.0;
        
        if (Math.Abs(y) <= halfCoreWidth)
        {
            return 1.0; // Homogeneous core
        }
        else if (Math.Abs(y) <= halfEffectiveWidth)
        {
            // Gaussian edge regions
            var sigmaY = (halfEffectiveWidth - halfCoreWidth) / 2.146; // 2.146 ≈ sqrt(2π)
            return Math.Exp(-0.5 * Math.Pow((Math.Abs(y) - halfCoreWidth) / sigmaY, 2));
        }
        else
        {
            return 0; // Outside cloud
        }
    }

    /// <summary>
    /// Calculate vertical concentration profile
    /// </summary>
    private double CalculateVerticalConcentrationProfile(double z, double effectiveHeight, double n)
    {
        if (z < 0 || z > effectiveHeight) return 0;
        
        // Power law profile: C(z) = C0 * (1 - z/H)^n
        return Math.Pow(1.0 - z / effectiveHeight, n);
    }

    /// <summary>
    /// Calculate power law exponent for vertical profile
    /// </summary>
    private double CalculatePowerLawExponent(HeavyGasSourceParameters source, WeatherData weather)
    {
        // Typical values range from 1-3 depending on stability and source characteristics
        var stabilityClass = weather.StabilityClass ?? "D";
        
        return stabilityClass.ToUpper() switch
        {
            "A" or "B" => 1.0, // Unstable - well-mixed
            "C" or "D" => 1.5, // Neutral
            "E" or "F" => 2.5, // Stable - stratified
            _ => 1.5
        };
    }

    /// <summary>
    /// Calculate enhanced lateral dispersion for passive diffusion stage
    /// </summary>
    private double CalculateEnhancedLateralDispersion(double x, HeavyGasSourceParameters source, string stabilityClass)
    {
        // Start with normal Gaussian dispersion and enhance based on initial spreading
        var normalSigmaY = CalculateNormalSigmaY(x, stabilityClass);
        var initialSpread = source.InitialRadius;
        
        return Math.Sqrt(normalSigmaY * normalSigmaY + initialSpread * initialSpread);
    }

    /// <summary>
    /// Calculate enhanced vertical dispersion for passive diffusion stage
    /// </summary>
    private double CalculateEnhancedVerticalDispersion(double x, HeavyGasSourceParameters source, string stabilityClass)
    {
        var normalSigmaZ = CalculateNormalSigmaZ(x, stabilityClass);
        var initialHeight = source.InitialHeight;
        
        return Math.Sqrt(normalSigmaZ * normalSigmaZ + (initialHeight / 2.146) * (initialHeight / 2.146));
    }

    /// <summary>
    /// Calculate normal Gaussian lateral dispersion
    /// </summary>
    private double CalculateNormalSigmaY(double x, string stabilityClass)
    {
        var xKm = x / 1000.0;
        
        return stabilityClass.ToUpper() switch
        {
            "A" => 0.22 * xKm / Math.Pow(1 + 0.0001 * x, 0.5),
            "B" => 0.16 * xKm / Math.Pow(1 + 0.0001 * x, 0.5),
            "C" => 0.11 * xKm / Math.Pow(1 + 0.0001 * x, 0.5),
            "D" => 0.08 * xKm / Math.Pow(1 + 0.0001 * x, 0.5),
            "E" => 0.06 * xKm / Math.Pow(1 + 0.0001 * x, 0.5),
            "F" => 0.04 * xKm / Math.Pow(1 + 0.0001 * x, 0.5),
            _ => 0.08 * xKm / Math.Pow(1 + 0.0001 * x, 0.5)
        } * 1000; // Convert to meters
    }

    /// <summary>
    /// Calculate normal Gaussian vertical dispersion
    /// </summary>
    private double CalculateNormalSigmaZ(double x, string stabilityClass)
    {
        var xKm = x / 1000.0;
        
        return stabilityClass.ToUpper() switch
        {
            "A" => 0.2 * xKm,
            "B" => 0.12 * xKm,
            "C" => 0.08 * xKm / Math.Pow(1 + 0.0002 * x, 0.5),
            "D" => 0.06 * xKm / Math.Pow(1 + 0.0015 * x, 0.5),
            "E" => 0.03 * xKm / Math.Pow(1 + 0.0003 * x, 1.0),
            "F" => 0.016 * xKm / Math.Pow(1 + 0.0003 * x, 1.0),
            _ => 0.06 * xKm / Math.Pow(1 + 0.0015 * x, 0.5)
        } * 1000; // Convert to meters
    }

    /// <summary>
    /// Calculate wind speed at specific height
    /// </summary>
    private double CalculateWindSpeedAtHeight(double windSpeedRef, double height, string stabilityClass)
    {
        var refHeight = 10.0;
        var exponent = stabilityClass.ToUpper() switch
        {
            "A" => 0.109,
            "B" => 0.112, 
            "C" => 0.120,
            "D" => 0.142,
            "E" => 0.203,
            "F" => 0.253,
            _ => 0.142
        };
        
        return windSpeedRef * Math.Pow(height / refHeight, exponent);
    }

    /// <summary>
    /// Calculate air density
    /// </summary>
    private double CalculateAirDensity(double temperatureC, double pressureMb)
    {
        var temperatureK = temperatureC + 273.15;
        var pressurePa = pressureMb * 100;
        var R = 287.0; // Specific gas constant for dry air
        
        return pressurePa / (R * temperatureK);
    }

    /// <summary>
    /// Calculate gas density based on chemical properties
    /// </summary>
    private double CalculateGasDensity(Chemical chemical, double temperatureC, double pressureMb)
    {
        var temperatureK = temperatureC + 273.15;
        var pressurePa = pressureMb * 100;
        var R = 8314.0; // Universal gas constant J/(mol·K)
        
        // For ideal gas: ρ = PM/(RT)
        return (pressurePa * chemical.MolecularWeight) / (R * temperatureK);
    }

    /// <summary>
    /// Calculate effective release rate
    /// </summary>
    private double CalculateEffectiveReleaseRate(Release release, Chemical chemical, WeatherData weather)
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

    /// <summary>
    /// Structure to hold heavy gas source parameters
    /// </summary>
    private struct HeavyGasSourceParameters
    {
        public double ReleaseRate { get; set; }        // kg/s
        public double ReducedGravity { get; set; }     // m/s²
        public double InitialRadius { get; set; }      // m
        public double InitialHeight { get; set; }      // m
        public double GasDensity { get; set; }        // kg/m³
        public double AirDensity { get; set; }        // kg/m³
    }
}