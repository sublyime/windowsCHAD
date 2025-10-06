using ChemicalDispersionModeling.Core.Models;

namespace ChemicalDispersionModeling.Core.Services;

/// <summary>
/// Service for fetching weather data from various sources
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// Get current weather data for a specific location
    /// </summary>
    Task<WeatherData?> GetCurrentWeatherAsync(double latitude, double longitude);
    
    /// <summary>
    /// Get weather forecast for a specific location and time range
    /// </summary>
    Task<IEnumerable<WeatherData>> GetWeatherForecastAsync(double latitude, double longitude, DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get weather data from National Weather Service
    /// </summary>
    Task<WeatherData?> GetNwsWeatherAsync(double latitude, double longitude);
    
    /// <summary>
    /// Get weather data from OpenMeteo
    /// </summary>
    Task<WeatherData?> GetOpenMeteoWeatherAsync(double latitude, double longitude);
    
    /// <summary>
    /// Calculate atmospheric stability class based on weather conditions
    /// </summary>
    string CalculateStabilityClass(double windSpeed, double cloudCover, bool isDaytime, double? solarRadiation = null);
}