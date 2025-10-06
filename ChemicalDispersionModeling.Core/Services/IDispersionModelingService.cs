using ChemicalDispersionModeling.Core.Models;

namespace ChemicalDispersionModeling.Core.Services;

/// <summary>
/// Service for dispersion modeling calculations
/// </summary>
public interface IDispersionModelingService
{
    /// <summary>
    /// Calculate dispersion using Gaussian plume model
    /// </summary>
    Task<IEnumerable<DispersionResult>> CalculateGaussianPlumeAsync(Release release, IEnumerable<Receptor> receptors, WeatherData weather);
    
    /// <summary>
    /// Calculate dispersion for a grid of points around the release
    /// </summary>
    Task<IEnumerable<DispersionResult>> CalculateDispersionGridAsync(Release release, WeatherData weather, double gridSize = 100, double maxDistance = 10000);
    
    /// <summary>
    /// Calculate time-varying dispersion for continuous releases
    /// </summary>
    Task<IEnumerable<DispersionResult>> CalculateTimeVaryingDispersionAsync(Release release, IEnumerable<Receptor> receptors, IEnumerable<WeatherData> weatherSequence);
    
    /// <summary>
    /// Calculate effective release rate considering physical state and conditions
    /// </summary>
    double CalculateEffectiveReleaseRate(Release release, Chemical chemical, WeatherData weather);
    
    /// <summary>
    /// Calculate plume rise due to buoyancy and momentum
    /// </summary>
    double CalculatePlumeRise(Release release, Chemical chemical, WeatherData weather);
    
    /// <summary>
    /// Calculate dispersion coefficients (σy and σz) based on stability class and distance
    /// </summary>
    (double SigmaY, double SigmaZ) CalculateDispersionCoefficients(string stabilityClass, double distance, double releaseHeight);
    
    /// <summary>
    /// Determine risk level based on concentration and toxicity thresholds
    /// </summary>
    string DetermineRiskLevel(double concentration, Chemical chemical);
}