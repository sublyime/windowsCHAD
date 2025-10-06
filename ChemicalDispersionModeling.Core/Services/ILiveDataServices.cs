using ChemicalDispersionModeling.Core.Models;

namespace ChemicalDispersionModeling.Core.Services;

/// <summary>
/// Interface for real-time weather data acquisition from various sources
/// </summary>
public interface ILiveWeatherService
{
    /// <summary>
    /// Get current weather from National Weather Service API
    /// </summary>
    Task<WeatherData?> GetNWSWeatherAsync(double latitude, double longitude);
    
    /// <summary>
    /// Get current weather from OpenMeteo API
    /// </summary>
    Task<WeatherData?> GetOpenMeteoWeatherAsync(double latitude, double longitude);
    
    /// <summary>
    /// Get weather from local weather station via specified protocol
    /// </summary>
    Task<WeatherData?> GetLocalWeatherAsync(string connectionString, DataSourceType sourceType);
    
    /// <summary>
    /// Start continuous monitoring of weather data
    /// </summary>
    Task StartContinuousMonitoringAsync(double latitude, double longitude, 
        DataSourceType primarySource, int intervalSeconds = 300);
    
    /// <summary>
    /// Stop continuous monitoring
    /// </summary>
    Task StopContinuousMonitoringAsync();
    
    /// <summary>
    /// Event fired when new weather data is received
    /// </summary>
    event EventHandler<WeatherData> WeatherDataReceived;
}

/// <summary>
/// Interface for real-time gas sensor data acquisition
/// </summary>
public interface IGasSensorService
{
    /// <summary>
    /// Get gas sensor readings from OSI PI server
    /// </summary>
    Task<IEnumerable<GasSensorReading>> GetOSIPIDataAsync(string serverUrl, string[] tagNames);
    
    /// <summary>
    /// Get gas sensor readings from OPC UA server
    /// </summary>
    Task<IEnumerable<GasSensorReading>> GetOPCUADataAsync(string endpointUrl, string[] nodeIds);
    
    /// <summary>
    /// Get gas sensor readings from MODBUS device
    /// </summary>
    Task<IEnumerable<GasSensorReading>> GetModbusDataAsync(string connectionString, int[] registerAddresses);
    
    /// <summary>
    /// Get gas sensor readings from serial device (NMEA 0183, etc.)
    /// </summary>
    Task<IEnumerable<GasSensorReading>> GetSerialDataAsync(string portName, int baudRate, string protocol);
    
    /// <summary>
    /// Get gas sensor readings from TCP/UDP network source
    /// </summary>
    Task<IEnumerable<GasSensorReading>> GetNetworkDataAsync(string host, int port, NetworkProtocol protocol);
    
    /// <summary>
    /// Get gas sensor readings from file source (CSV, XML, JSON)
    /// </summary>
    Task<IEnumerable<GasSensorReading>> GetFileDataAsync(string filePath, FileFormat format);
    
    /// <summary>
    /// Start continuous monitoring of gas sensors
    /// </summary>
    Task StartContinuousMonitoringAsync(DataSourceConfiguration config);
    
    /// <summary>
    /// Stop continuous monitoring
    /// </summary>
    Task StopContinuousMonitoringAsync();
    
    /// <summary>
    /// Event fired when new gas sensor data is received
    /// </summary>
    event EventHandler<IEnumerable<GasSensorReading>> GasSensorDataReceived;
}

/// <summary>
/// Data source types for weather and gas sensor connections
/// </summary>
public enum DataSourceType
{
    NationalWeatherService,
    OpenMeteo,
    SerialPort,
    TCPIP,
    UDP,
    OSIPI,
    OPCUA,
    Modbus,
    File,
    Database
}

/// <summary>
/// Network protocols for data transmission
/// </summary>
public enum NetworkProtocol
{
    TCP,
    UDP,
    HTTP,
    HTTPS,
    WebSocket
}

/// <summary>
/// File formats for data import
/// </summary>
public enum FileFormat
{
    CSV,
    JSON,
    XML,
    Excel,
    Binary
}

/// <summary>
/// Configuration for data source connections
/// </summary>
public class DataSourceConfiguration
{
    public string Name { get; set; } = string.Empty;
    public DataSourceType SourceType { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public int PollingIntervalSeconds { get; set; } = 30;
    public Dictionary<string, string> Parameters { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Gas sensor reading data structure
/// </summary>
public class GasSensorReading
{
    public DateTime Timestamp { get; set; }
    public string SensorId { get; set; } = string.Empty;
    public string SensorLocation { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Elevation { get; set; }
    public string ChemicalName { get; set; } = string.Empty;
    public double Concentration { get; set; }
    public string ConcentrationUnit { get; set; } = "ppm";
    public double? Temperature { get; set; }
    public double? Humidity { get; set; }
    public double? WindSpeed { get; set; }
    public double? WindDirection { get; set; }
    public string QualityFlag { get; set; } = "Good";
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}