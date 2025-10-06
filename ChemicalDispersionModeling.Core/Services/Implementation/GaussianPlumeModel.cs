using ChemicalDispersionModeling.Core.Models;
using Microsoft.Extensions.Logging;

namespace ChemicalDispersionModeling.Core.Services.Implementation;

/// <summary>
/// NOAA ALOHA-compliant Gaussian plume dispersion model implementation
/// Based on NOAA Technical Memorandum NOS OR&R 43 Section 4.3
/// </summary>
public class GaussianPlumeModel
{
    private readonly ILogger<GaussianPlumeModel> _logger;

    public GaussianPlumeModel(ILogger<GaussianPlumeModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculate concentration using ALOHA Gaussian plume model
    /// Reference: ALOHA Technical Documentation Section 4.3
    /// </summary>
    public double CalculateConcentration(double x, double y, double z, Release release, Chemical chemical, WeatherData weather)
    {
        if (x <= 0 || weather.WindSpeed <= 0) return 0;

        var stabilityClass = weather.StabilityClass ?? "D";
        var isUrban = DetermineUrbanRural(0.1); // Default to rural for now
        
        // Get dispersion parameters using Briggs formulations
        var (sigmaY, sigmaZ) = CalculateDispersionParameters(x, stabilityClass, isUrban);
        var sigmaX = CalculateAlongWindDispersion(x, stabilityClass);

        // Calculate effective release rate
        var Q = CalculateEffectiveReleaseRate(release, chemical, weather);
        
        // Wind speed at release height
        var windSpeed = CalculateWindSpeedAtHeight(weather.WindSpeed, release.ReleaseHeight, stabilityClass, 0.03);

        // Calculate Gaussian concentration components
        var lateralTerm = CalculateLateralTerm(y, sigmaY);
        var verticalTerm = CalculateVerticalTerm(z, release.ReleaseHeight, sigmaZ, 0); // No inversion
        
        // ALOHA Gaussian formula: C = Q / (U * σy * σz * √(2π)) * exp(-y²/2σy²) * [vertical reflection terms]
        var concentration = (Q / (windSpeed * sigmaY * sigmaZ * Math.Sqrt(2 * Math.PI))) * 
                           lateralTerm * verticalTerm;

        return Math.Max(0, concentration * 1e6); // Convert to mg/m³
    }

    /// <summary>
    /// Calculate dispersion parameters using Briggs formulations
    /// Reference: ALOHA Technical Documentation Section 4.3.1, Table 13
    /// </summary>
    public (double SigmaY, double SigmaZ) CalculateDispersionParameters(double x, string stabilityClass, bool isUrban)
    {
        var xKm = x / 1000.0; // Convert to km for Briggs formulations
        
        // Briggs coefficients from ALOHA Table 13
        var coefficients = GetBriggsCoefficients(stabilityClass, isUrban);
        
        // Briggs formulations: σy = sy1 * x^(1/(1 + sy2*x))  and  σz = sz1 * x^(1/(1 + sz2*x + sz3*x^2))
        var sigmaY = coefficients.SY1 * Math.Pow(xKm, 1.0 / (1.0 + coefficients.SY2 * xKm));
        
        var denominatorZ = 1.0 + coefficients.SZ2 * xKm + coefficients.SZ3 * xKm * xKm;
        var sigmaZ = coefficients.SZ1 * Math.Pow(xKm, 1.0 / denominatorZ);
        
        // Convert back to meters and apply minimum values
        sigmaY = Math.Max(1.0, sigmaY * 1000.0);
        sigmaZ = Math.Max(1.0, sigmaZ * 1000.0);
        
        return (sigmaY, sigmaZ);
    }

    /// <summary>
    /// Calculate along-wind dispersion parameter
    /// Reference: ALOHA Technical Documentation Section 4.3.1
    /// </summary>
    public double CalculateAlongWindDispersion(double x, string stabilityClass)
    {
        var coefficients = GetBriggsCoefficients(stabilityClass, false);
        var xKm = x / 1000.0;
        
        // σx = sx1 * x^(sx2) formula from ALOHA
        var sigmaX = coefficients.SX1 * Math.Pow(xKm, coefficients.SX2);
        
        return Math.Max(1.0, sigmaX * 1000.0); // Convert to meters
    }

    /// <summary>
    /// Calculate lateral Gaussian term
    /// </summary>
    private double CalculateLateralTerm(double y, double sigmaY)
    {
        return Math.Exp(-0.5 * Math.Pow(y / sigmaY, 2));
    }

    /// <summary>
    /// Calculate vertical term with ground reflection
    /// Reference: ALOHA Technical Documentation Section 4.3
    /// </summary>
    private double CalculateVerticalTerm(double z, double releaseHeight, double sigmaZ, double inversionHeight)
    {
        if (inversionHeight > 0 && inversionHeight > releaseHeight)
        {
            // With inversion - use reflection series (simplified to 5 reflections as per ALOHA)
            double sum = 0;
            for (int n = -5; n <= 5; n++)
            {
                // Ground reflections
                var h1 = releaseHeight + 2 * n * inversionHeight;
                var h2 = -releaseHeight + 2 * n * inversionHeight;
                
                sum += Math.Exp(-0.5 * Math.Pow((z - h1) / sigmaZ, 2));
                sum += Math.Exp(-0.5 * Math.Pow((z - h2) / sigmaZ, 2));
            }
            return sum;
        }
        else
        {
            // No inversion - simple ground reflection
            var directTerm = Math.Exp(-0.5 * Math.Pow((z - releaseHeight) / sigmaZ, 2));
            var reflectedTerm = Math.Exp(-0.5 * Math.Pow((z + releaseHeight) / sigmaZ, 2));
            
            return directTerm + reflectedTerm;
        }
    }

    /// <summary>
    /// Calculate wind speed at specific height using power law profile
    /// Reference: ALOHA Technical Documentation Section 4.2.3
    /// </summary>
    private double CalculateWindSpeedAtHeight(double windSpeedRef, double height, string stabilityClass, double roughnessLength)
    {
        var refHeight = 10.0; // Reference height (10m)
        var exponent = GetWindProfileExponent(stabilityClass, roughnessLength);
        
        return windSpeedRef * Math.Pow(height / refHeight, exponent);
    }

    /// <summary>
    /// Get wind profile exponent based on stability class
    /// Reference: ALOHA Technical Documentation Section 4.2.3
    /// </summary>
    private double GetWindProfileExponent(string stabilityClass, double roughnessLength)
    {
        // Typical values from ALOHA for z0 = 0.15 cm
        return stabilityClass.ToUpper() switch
        {
            "A" => 0.109,
            "B" => 0.112,
            "C" => 0.120,
            "D" => 0.142,
            "E" => 0.203,
            "F" => 0.253,
            _ => 0.142 // Default to neutral
        };
    }

    /// <summary>
    /// Calculate effective release rate considering physical state and conditions
    /// Reference: ALOHA Technical Documentation Chapter 3
    /// </summary>
    private double CalculateEffectiveReleaseRate(Release release, Chemical chemical, WeatherData weather)
    {
        if (release.ReleaseRate.HasValue)
        {
            return release.ReleaseRate.Value; // kg/s
        }
        
        if (release.TotalMass.HasValue)
        {
            var duration = (release.EndTime - release.StartTime)?.TotalSeconds ?? 3600; // Default 1 hour
            return release.TotalMass.Value / duration; // kg/s
        }
        
        // If no mass specified, use a default small release
        _logger.LogWarning("No release rate or total mass specified, using default 0.1 kg/s");
        return 0.1;
    }

    /// <summary>
    /// Determine if location is urban or rural based on roughness length
    /// Reference: ALOHA Technical Documentation Section 4.3.1
    /// </summary>
    private bool DetermineUrbanRural(double roughnessLength)
    {
        // ALOHA uses 20 cm as the threshold
        return roughnessLength >= 0.20;
    }

    /// <summary>
    /// Get Briggs coefficients for dispersion parameter calculations
    /// Reference: ALOHA Technical Documentation Table 13
    /// </summary>
    private BriggsCoefficients GetBriggsCoefficients(string stabilityClass, bool isUrban)
    {
        return stabilityClass.ToUpper() switch
        {
            "A" => new BriggsCoefficients
            {
                SX1 = 0.02, SX2 = 1.22,
                SY1 = 0.22, SY2 = 0.0001,
                SZ1 = isUrban ? 0.24 : 0.2, SZ2 = 0, SZ3 = 0
            },
            "B" => new BriggsCoefficients
            {
                SX1 = 0.02, SX2 = 1.22,
                SY1 = 0.16, SY2 = 0.0001,
                SZ1 = isUrban ? 0.24 : 0.12, SZ2 = 0, SZ3 = 0
            },
            "C" => new BriggsCoefficients
            {
                SX1 = 0.02, SX2 = 1.22,
                SY1 = 0.11, SY2 = 0.0001,
                SZ1 = isUrban ? 0.20 : 0.08, SZ2 = isUrban ? 0.0003 : 0.0002, SZ3 = isUrban ? 0 : -0.5
            },
            "D" => new BriggsCoefficients
            {
                SX1 = 0.04, SX2 = 1.14,
                SY1 = 0.08, SY2 = 0.0001,
                SZ1 = isUrban ? 0.14 : 0.06, SZ2 = isUrban ? 0.0003 : 0.0015, SZ3 = isUrban ? -0.5 : -0.5
            },
            "E" => new BriggsCoefficients
            {
                SX1 = 0.17, SX2 = 0.97,
                SY1 = 0.06, SY2 = 0.0001,
                SZ1 = isUrban ? 0.08 : 0.03, SZ2 = isUrban ? 0.0015 : 0.0003, SZ3 = isUrban ? -0.5 : -1
            },
            "F" => new BriggsCoefficients
            {
                SX1 = 0.17, SX2 = 0.97,
                SY1 = 0.04, SY2 = 0.0001,
                SZ1 = isUrban ? 0.05 : 0.016, SZ2 = isUrban ? 0.0003 : 0.0003, SZ3 = isUrban ? -1 : -1
            },
            _ => new BriggsCoefficients // Default to D (neutral)
            {
                SX1 = 0.04, SX2 = 1.14,
                SY1 = 0.08, SY2 = 0.0001,
                SZ1 = 0.06, SZ2 = 0.0015, SZ3 = -0.5
            }
        };
    }

    /// <summary>
    /// Structure to hold Briggs coefficients
    /// </summary>
    private struct BriggsCoefficients
    {
        public double SX1, SX2; // Along-wind dispersion coefficients
        public double SY1, SY2; // Cross-wind dispersion coefficients  
        public double SZ1, SZ2, SZ3; // Vertical dispersion coefficients
    }
}