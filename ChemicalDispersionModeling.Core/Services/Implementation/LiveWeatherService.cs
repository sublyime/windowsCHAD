using ChemicalDispersionModeling.Core.Models;
using ChemicalDispersionModeling.Core.Services;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ChemicalDispersionModeling.Core.Services.Implementation;

public class LiveWeatherService : ILiveWeatherService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LiveWeatherService> _logger;
    private Timer? _monitoringTimer;
    private SerialPort? _serialPort;
    private TcpClient? _tcpClient;
    private UdpClient? _udpClient;
    private bool _isMonitoring;
    private DataSourceType _currentSource;
    private double _currentLatitude;
    private double _currentLongitude;

    public event EventHandler<WeatherData>? WeatherDataReceived;

    public LiveWeatherService(HttpClient httpClient, ILogger<LiveWeatherService> logger)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ChemicalDispersionApp/1.0");
        _logger = logger;
    }

    public async Task<WeatherData?> GetNWSWeatherAsync(double latitude, double longitude)
    {
        try
        {
            _logger.LogInformation($"Fetching NWS weather for {latitude}, {longitude}");
            
            // First get the grid point
            var gridUrl = $"https://api.weather.gov/points/{latitude},{longitude}";
            var gridResponse = await _httpClient.GetAsync(gridUrl);
            
            if (!gridResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Failed to get NWS grid point: {gridResponse.StatusCode}");
                return null;
            }

            var gridJson = await gridResponse.Content.ReadAsStringAsync();
            var gridData = JsonDocument.Parse(gridJson);
            
            var forecastUrl = gridData.RootElement
                .GetProperty("properties")
                .GetProperty("forecast")
                .GetString();

            if (string.IsNullOrEmpty(forecastUrl))
            {
                _logger.LogWarning("No forecast URL found in NWS response");
                return null;
            }

            // Get current conditions from the forecast
            var forecastResponse = await _httpClient.GetAsync(forecastUrl);
            
            if (!forecastResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Failed to get NWS forecast: {forecastResponse.StatusCode}");
                return null;
            }

            var forecastJson = await forecastResponse.Content.ReadAsStringAsync();
            var forecastData = JsonDocument.Parse(forecastJson);
            
            var currentPeriod = forecastData.RootElement
                .GetProperty("properties")
                .GetProperty("periods")[0];

            // Extract weather data (NWS doesn't provide all real-time data in basic forecast)
            var weatherData = new WeatherData
            {
                Timestamp = DateTime.UtcNow,
                Latitude = latitude,
                Longitude = longitude,
                Temperature = ExtractTemperatureFromText(currentPeriod.GetProperty("detailedForecast").GetString() ?? ""),
                WindSpeed = ExtractWindSpeedFromText(currentPeriod.GetProperty("detailedForecast").GetString() ?? ""),
                WindDirection = ExtractWindDirectionFromText(currentPeriod.GetProperty("windDirection").GetString() ?? ""),
                CloudCover = 50.0, // Default estimate
                Visibility = 10000.0, // Default 10km
                DataSource = "National Weather Service",
                QualityFlag = "Estimated"
            };

            _logger.LogInformation($"Retrieved NWS weather: {weatherData.Temperature}°C, Wind: {weatherData.WindSpeed} m/s");
            return weatherData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching NWS weather data");
            return null;
        }
    }

    public async Task<WeatherData?> GetOpenMeteoWeatherAsync(double latitude, double longitude)
    {
        try
        {
            _logger.LogInformation($"Fetching OpenMeteo weather for {latitude}, {longitude}");
            
            var url = $"https://api.open-meteo.com/v1/forecast?" +
                     $"latitude={latitude}&longitude={longitude}" +
                     "&current=temperature_2m,relative_humidity_2m,wind_speed_10m,wind_direction_10m,cloud_cover,visibility" +
                     "&wind_speed_unit=ms&timezone=auto";

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Failed to get OpenMeteo data: {response.StatusCode}");
                return null;
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(jsonString);
            
            var current = data.RootElement.GetProperty("current");
            
            var weatherData = new WeatherData
            {
                Timestamp = DateTime.UtcNow,
                Latitude = latitude,
                Longitude = longitude,
                Temperature = current.GetProperty("temperature_2m").GetDouble(),
                Humidity = current.GetProperty("relative_humidity_2m").GetDouble(),
                WindSpeed = current.GetProperty("wind_speed_10m").GetDouble(),
                WindDirection = current.GetProperty("wind_direction_10m").GetDouble(),
                CloudCover = current.GetProperty("cloud_cover").GetDouble(),
                Visibility = current.GetProperty("visibility").GetDouble(),
                DataSource = "OpenMeteo API",
                QualityFlag = "Good"
            };

            _logger.LogInformation($"Retrieved OpenMeteo weather: {weatherData.Temperature}°C, Wind: {weatherData.WindSpeed} m/s");
            return weatherData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching OpenMeteo weather data");
            return null;
        }
    }

    public async Task<WeatherData?> GetLocalWeatherAsync(string connectionString, DataSourceType sourceType)
    {
        try
        {
            _logger.LogInformation($"Fetching local weather from {sourceType}: {connectionString}");
            
            return sourceType switch
            {
                DataSourceType.SerialPort => await GetSerialWeatherAsync(connectionString),
                DataSourceType.TCPIP => await GetTcpWeatherAsync(connectionString),
                DataSourceType.UDP => await GetUdpWeatherAsync(connectionString),
                DataSourceType.File => await GetFileWeatherAsync(connectionString),
                _ => throw new NotSupportedException($"Data source type {sourceType} not supported for weather data")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching local weather data from {sourceType}");
            return null;
        }
    }

    public async Task StartContinuousMonitoringAsync(double latitude, double longitude, 
        DataSourceType primarySource, int intervalSeconds = 300)
    {
        if (_isMonitoring)
        {
            await StopContinuousMonitoringAsync();
        }

        _currentLatitude = latitude;
        _currentLongitude = longitude;
        _currentSource = primarySource;
        _isMonitoring = true;

        _logger.LogInformation($"Starting continuous weather monitoring: {primarySource}, interval: {intervalSeconds}s");

        _monitoringTimer = new Timer(async _ => await MonitorWeatherAsync(), 
            null, TimeSpan.Zero, TimeSpan.FromSeconds(intervalSeconds));
    }

    public async Task StopContinuousMonitoringAsync()
    {
        _isMonitoring = false;
        _monitoringTimer?.Dispose();
        _serialPort?.Close();
        _tcpClient?.Close();
        _udpClient?.Close();
        
        _logger.LogInformation("Stopped continuous weather monitoring");
        await Task.CompletedTask;
    }

    private async Task MonitorWeatherAsync()
    {
        if (!_isMonitoring) return;

        try
        {
            WeatherData? weatherData = _currentSource switch
            {
                DataSourceType.NationalWeatherService => await GetNWSWeatherAsync(_currentLatitude, _currentLongitude),
                DataSourceType.OpenMeteo => await GetOpenMeteoWeatherAsync(_currentLatitude, _currentLongitude),
                _ => null
            };

            if (weatherData != null)
            {
                WeatherDataReceived?.Invoke(this, weatherData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during weather monitoring");
        }
    }

    private async Task<WeatherData?> GetSerialWeatherAsync(string connectionString)
    {
        // Parse connection string: COM3:9600:8:N:1
        var parts = connectionString.Split(':');
        if (parts.Length < 2) return null;

        var portName = parts[0];
        var baudRate = int.Parse(parts[1]);

        try
        {
            _serialPort = new SerialPort(portName, baudRate);
            _serialPort.Open();
            
            // Send request for weather data (NMEA 0183 style)
            _serialPort.WriteLine("$WIMWV,,,,,*hh\r\n");
            
            // Read response
            var response = await Task.Run(() => _serialPort.ReadLine());
            _serialPort.Close();

            return ParseNMEAWeatherData(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading serial weather data from {portName}");
            return null;
        }
    }

    private async Task<WeatherData?> GetTcpWeatherAsync(string connectionString)
    {
        // Parse connection string: 192.168.1.100:8080
        var parts = connectionString.Split(':');
        if (parts.Length != 2) return null;

        var host = parts[0];
        var port = int.Parse(parts[1]);

        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(host, port);
            
            var stream = _tcpClient.GetStream();
            var request = Encoding.UTF8.GetBytes("GET_WEATHER\r\n");
            await stream.WriteAsync(request, 0, request.Length);
            
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            
            _tcpClient.Close();
            
            return ParseJSONWeatherData(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading TCP weather data from {host}:{port}");
            return null;
        }
    }

    private async Task<WeatherData?> GetUdpWeatherAsync(string connectionString)
    {
        // Parse connection string: 192.168.1.100:8080
        var parts = connectionString.Split(':');
        if (parts.Length != 2) return null;

        var host = parts[0];
        var port = int.Parse(parts[1]);

        try
        {
            _udpClient = new UdpClient();
            var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
            
            var request = Encoding.UTF8.GetBytes("GET_WEATHER");
            await _udpClient.SendAsync(request, request.Length, endpoint);
            
            var response = await _udpClient.ReceiveAsync();
            var data = Encoding.UTF8.GetString(response.Buffer);
            
            _udpClient.Close();
            
            return ParseJSONWeatherData(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading UDP weather data from {host}:{port}");
            return null;
        }
    }

    private async Task<WeatherData?> GetFileWeatherAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return null;
            
            var content = await File.ReadAllTextAsync(filePath);
            
            if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return ParseJSONWeatherData(content);
            }
            else if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return ParseCSVWeatherData(content);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading weather data from file {filePath}");
            return null;
        }
    }

    private WeatherData? ParseNMEAWeatherData(string nmeaData)
    {
        // Simple NMEA 0183 parsing for wind data
        if (string.IsNullOrEmpty(nmeaData) || !nmeaData.StartsWith("$")) return null;
        
        var parts = nmeaData.Split(',');
        if (parts.Length < 6) return null;

        try
        {
            return new WeatherData
            {
                Timestamp = DateTime.UtcNow,
                WindDirection = double.Parse(parts[1]),
                WindSpeed = double.Parse(parts[3]) * 0.514444, // Convert knots to m/s
                DataSource = "NMEA 0183 Serial",
                QualityFlag = "Good"
            };
        }
        catch
        {
            return null;
        }
    }

    private WeatherData? ParseJSONWeatherData(string jsonData)
    {
        try
        {
            var data = JsonDocument.Parse(jsonData);
            var root = data.RootElement;
            
            return new WeatherData
            {
                Timestamp = DateTime.UtcNow,
                Temperature = root.TryGetProperty("temperature", out var temp) ? temp.GetDouble() : 0,
                Humidity = root.TryGetProperty("humidity", out var hum) ? hum.GetDouble() : 0,
                Pressure = root.TryGetProperty("pressure", out var press) ? press.GetDouble() : 0,
                WindSpeed = root.TryGetProperty("windSpeed", out var ws) ? ws.GetDouble() : 0,
                WindDirection = root.TryGetProperty("windDirection", out var wd) ? wd.GetDouble() : 0,
                DataSource = "JSON Local Source",
                QualityFlag = "Good"
            };
        }
        catch
        {
            return null;
        }
    }

    private WeatherData? ParseCSVWeatherData(string csvData)
    {
        try
        {
            var lines = csvData.Split('\n');
            if (lines.Length < 2) return null;
            
            var values = lines[1].Split(','); // Skip header, take first data row
            if (values.Length < 5) return null;
            
            return new WeatherData
            {
                Timestamp = DateTime.UtcNow,
                Temperature = double.Parse(values[1]),
                Humidity = double.Parse(values[2]),
                WindSpeed = double.Parse(values[3]),
                WindDirection = double.Parse(values[4]),
                DataSource = "CSV File",
                QualityFlag = "Good"
            };
        }
        catch
        {
            return null;
        }
    }

    private double ExtractTemperatureFromText(string text)
    {
        // Simple extraction - in real implementation, use regex
        return 20.0; // Default value
    }

    private double ExtractWindSpeedFromText(string text)
    {
        // Simple extraction - in real implementation, use regex
        return 5.0; // Default value
    }

    private double ExtractWindDirectionFromText(string direction)
    {
        return direction.ToUpper() switch
        {
            "N" => 0,
            "NE" => 45,
            "E" => 90,
            "SE" => 135,
            "S" => 180,
            "SW" => 225,
            "W" => 270,
            "NW" => 315,
            _ => 0
        };
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        _serialPort?.Dispose();
        _tcpClient?.Dispose();
        _udpClient?.Dispose();
        _httpClient?.Dispose();
    }
}