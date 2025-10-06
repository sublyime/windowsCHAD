using ChemicalDispersionModeling.Core.Services;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ChemicalDispersionModeling.Core.Services.Implementation;

public class GasSensorService : IGasSensorService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GasSensorService> _logger;
    private Timer? _monitoringTimer;
    private readonly List<DataSourceConfiguration> _activeConfigurations = new();
    private bool _isMonitoring;

    public event EventHandler<IEnumerable<GasSensorReading>>? GasSensorDataReceived;

    public GasSensorService(ILogger<GasSensorService> logger)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ChemicalDispersionApp/1.0");
        _logger = logger;
    }

    public async Task<IEnumerable<GasSensorReading>> GetOSIPIDataAsync(string serverUrl, string[] tagNames)
    {
        try
        {
            _logger.LogInformation($"Fetching OSI PI data from {serverUrl}");
            
            var readings = new List<GasSensorReading>();
            
            // OSI PI Web API call (simplified)
            foreach (var tagName in tagNames)
            {
                var url = $"{serverUrl}/piwebapi/points?path=\\\\{tagName}&webid";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonData = await response.Content.ReadAsStringAsync();
                    var reading = ParseOSIPIData(jsonData, tagName);
                    if (reading != null) readings.Add(reading);
                }
            }
            
            _logger.LogInformation($"Retrieved {readings.Count} OSI PI readings");
            return readings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching OSI PI data");
            return Enumerable.Empty<GasSensorReading>();
        }
    }

    public async Task<IEnumerable<GasSensorReading>> GetOPCUADataAsync(string endpointUrl, string[] nodeIds)
    {
        try
        {
            _logger.LogInformation($"Fetching OPC UA data from {endpointUrl}");
            
            var readings = new List<GasSensorReading>();
            
            // OPC UA client implementation (simplified - would use OPC UA library)
            // For now, simulate the data structure
            foreach (var nodeId in nodeIds)
            {
                var reading = new GasSensorReading
                {
                    Timestamp = DateTime.UtcNow,
                    SensorId = nodeId,
                    SensorLocation = "OPC UA Sensor",
                    ChemicalName = ExtractChemicalFromNodeId(nodeId),
                    Concentration = await SimulateOPCUAReading(nodeId),
                    ConcentrationUnit = "ppm",
                    QualityFlag = "Good"
                };
                
                readings.Add(reading);
            }
            
            _logger.LogInformation($"Retrieved {readings.Count} OPC UA readings");
            return readings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching OPC UA data");
            return Enumerable.Empty<GasSensorReading>();
        }
    }

    public async Task<IEnumerable<GasSensorReading>> GetModbusDataAsync(string connectionString, int[] registerAddresses)
    {
        try
        {
            _logger.LogInformation($"Fetching Modbus data: {connectionString}");
            
            var readings = new List<GasSensorReading>();
            
            // Parse connection string: 192.168.1.100:502:1 (IP:Port:SlaveId)
            var parts = connectionString.Split(':');
            if (parts.Length < 3) return readings;
            
            var host = parts[0];
            var port = int.Parse(parts[1]);
            var slaveId = byte.Parse(parts[2]);
            
            // Modbus TCP implementation (simplified)
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port);
            var stream = tcpClient.GetStream();
            
            foreach (var register in registerAddresses)
            {
                var modbusRequest = BuildModbusReadRequest(slaveId, register, 1);
                await stream.WriteAsync(modbusRequest, 0, modbusRequest.Length);
                
                var response = new byte[12];
                await stream.ReadAsync(response, 0, response.Length);
                
                var value = ParseModbusResponse(response);
                
                var reading = new GasSensorReading
                {
                    Timestamp = DateTime.UtcNow,
                    SensorId = $"Modbus_{slaveId}_{register}",
                    SensorLocation = $"Modbus Device {host}",
                    ChemicalName = DetermineChemicalFromRegister(register),
                    Concentration = value,
                    ConcentrationUnit = "ppm",
                    QualityFlag = "Good"
                };
                
                readings.Add(reading);
            }
            
            _logger.LogInformation($"Retrieved {readings.Count} Modbus readings");
            return readings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Modbus data");
            return Enumerable.Empty<GasSensorReading>();
        }
    }

    public async Task<IEnumerable<GasSensorReading>> GetSerialDataAsync(string portName, int baudRate, string protocol)
    {
        try
        {
            _logger.LogInformation($"Fetching serial data: {portName} at {baudRate} baud, protocol: {protocol}");
            
            var readings = new List<GasSensorReading>();
            
            using var serialPort = new SerialPort(portName, baudRate);
            serialPort.Open();
            
            if (protocol.ToUpper() == "NMEA0183")
            {
                // Send NMEA request
                serialPort.WriteLine("$GPGGA,*hh\r\n");
                var response = await Task.Run(() => serialPort.ReadLine());
                
                var reading = ParseNMEAGasData(response);
                if (reading != null) readings.Add(reading);
            }
            else
            {
                // Generic serial protocol
                serialPort.WriteLine("READ_GAS\r\n");
                var response = await Task.Run(() => serialPort.ReadLine());
                
                var reading = ParseGenericSerialData(response, portName);
                if (reading != null) readings.Add(reading);
            }
            
            _logger.LogInformation($"Retrieved {readings.Count} serial readings");
            return readings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching serial data");
            return Enumerable.Empty<GasSensorReading>();
        }
    }

    public async Task<IEnumerable<GasSensorReading>> GetNetworkDataAsync(string host, int port, NetworkProtocol protocol)
    {
        try
        {
            _logger.LogInformation($"Fetching network data: {protocol} {host}:{port}");
            
            var readings = new List<GasSensorReading>();
            
            switch (protocol)
            {
                case NetworkProtocol.TCP:
                    readings.AddRange(await GetTcpGasDataAsync(host, port));
                    break;
                case NetworkProtocol.UDP:
                    readings.AddRange(await GetUdpGasDataAsync(host, port));
                    break;
                case NetworkProtocol.HTTP:
                case NetworkProtocol.HTTPS:
                    readings.AddRange(await GetHttpGasDataAsync($"{protocol.ToString().ToLower()}://{host}:{port}"));
                    break;
            }
            
            _logger.LogInformation($"Retrieved {readings.Count} network readings");
            return readings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching network data");
            return Enumerable.Empty<GasSensorReading>();
        }
    }

    public async Task<IEnumerable<GasSensorReading>> GetFileDataAsync(string filePath, FileFormat format)
    {
        try
        {
            _logger.LogInformation($"Reading gas data from file: {filePath} (format: {format})");
            
            if (!File.Exists(filePath)) return Enumerable.Empty<GasSensorReading>();
            
            var content = await File.ReadAllTextAsync(filePath);
            
            return format switch
            {
                FileFormat.CSV => ParseCSVGasData(content),
                FileFormat.JSON => ParseJSONGasData(content),
                FileFormat.XML => ParseXMLGasData(content),
                _ => Enumerable.Empty<GasSensorReading>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file data");
            return Enumerable.Empty<GasSensorReading>();
        }
    }

    public async Task StartContinuousMonitoringAsync(DataSourceConfiguration config)
    {
        if (_isMonitoring)
        {
            await StopContinuousMonitoringAsync();
        }

        _activeConfigurations.Add(config);
        _isMonitoring = true;

        _logger.LogInformation($"Starting continuous gas sensor monitoring: {config.Name}");

        _monitoringTimer = new Timer(async _ => await MonitorAllSourcesAsync(),
            null, TimeSpan.Zero, TimeSpan.FromSeconds(config.PollingIntervalSeconds));
    }

    public async Task StopContinuousMonitoringAsync()
    {
        _isMonitoring = false;
        _monitoringTimer?.Dispose();
        _activeConfigurations.Clear();
        
        _logger.LogInformation("Stopped continuous gas sensor monitoring");
        await Task.CompletedTask;
    }

    private async Task MonitorAllSourcesAsync()
    {
        if (!_isMonitoring) return;

        var allReadings = new List<GasSensorReading>();

        foreach (var config in _activeConfigurations.Where(c => c.IsEnabled))
        {
            try
            {
                var readings = await GetDataFromConfiguration(config);
                allReadings.AddRange(readings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error monitoring source {config.Name}");
            }
        }

        if (allReadings.Any())
        {
            GasSensorDataReceived?.Invoke(this, allReadings);
        }
    }

    private async Task<IEnumerable<GasSensorReading>> GetDataFromConfiguration(DataSourceConfiguration config)
    {
        return config.SourceType switch
        {
            DataSourceType.OSIPI => await GetOSIPIDataAsync(config.ConnectionString, 
                config.Parameters.ContainsKey("tags") ? config.Parameters["tags"].Split(',') : Array.Empty<string>()),
            DataSourceType.OPCUA => await GetOPCUADataAsync(config.ConnectionString,
                config.Parameters.ContainsKey("nodeIds") ? config.Parameters["nodeIds"].Split(',') : Array.Empty<string>()),
            DataSourceType.Modbus => await GetModbusDataAsync(config.ConnectionString,
                config.Parameters.ContainsKey("registers") ? ParseIntArray(config.Parameters["registers"]) : Array.Empty<int>()),
            DataSourceType.SerialPort => await GetSerialDataAsync(
                config.ConnectionString.Split(':')[0],
                int.Parse(config.ConnectionString.Split(':')[1]),
                config.Parameters.GetValueOrDefault("protocol", "generic")),
            DataSourceType.File => await GetFileDataAsync(config.ConnectionString,
                Enum.Parse<FileFormat>(config.Parameters.GetValueOrDefault("format", "CSV"), true)),
            _ => Enumerable.Empty<GasSensorReading>()
        };
    }

    private async Task<IEnumerable<GasSensorReading>> GetTcpGasDataAsync(string host, int port)
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port);
        
        var stream = tcpClient.GetStream();
        var request = Encoding.UTF8.GetBytes("GET_GAS_DATA\r\n");
        await stream.WriteAsync(request, 0, request.Length);
        
        var buffer = new byte[1024];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        return ParseJSONGasData(response);
    }

    private async Task<IEnumerable<GasSensorReading>> GetUdpGasDataAsync(string host, int port)
    {
        using var udpClient = new UdpClient();
        var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
        
        var request = Encoding.UTF8.GetBytes("GET_GAS_DATA");
        await udpClient.SendAsync(request, request.Length, endpoint);
        
        var response = await udpClient.ReceiveAsync();
        var data = Encoding.UTF8.GetString(response.Buffer);
        
        return ParseJSONGasData(data);
    }

    private async Task<IEnumerable<GasSensorReading>> GetHttpGasDataAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return Enumerable.Empty<GasSensorReading>();
        
        var jsonData = await response.Content.ReadAsStringAsync();
        return ParseJSONGasData(jsonData);
    }

    private GasSensorReading? ParseOSIPIData(string jsonData, string tagName)
    {
        try
        {
            var data = JsonDocument.Parse(jsonData);
            var value = data.RootElement.GetProperty("Value").GetDouble();
            
            return new GasSensorReading
            {
                Timestamp = DateTime.UtcNow,
                SensorId = tagName,
                ChemicalName = ExtractChemicalFromTagName(tagName),
                Concentration = value,
                ConcentrationUnit = "ppm",
                QualityFlag = "Good"
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<double> SimulateOPCUAReading(string nodeId)
    {
        // Simulate OPC UA reading - in real implementation, use OPC UA client library
        await Task.Delay(50);
        return Random.Shared.NextDouble() * 100;
    }

    private byte[] BuildModbusReadRequest(byte slaveId, int register, int count)
    {
        // Simplified Modbus TCP request
        return new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, slaveId, 0x03, 
                           (byte)(register >> 8), (byte)(register & 0xFF), 
                           (byte)(count >> 8), (byte)(count & 0xFF) };
    }

    private double ParseModbusResponse(byte[] response)
    {
        if (response.Length < 12) return 0;
        return (response[9] << 8) | response[10];
    }

    private GasSensorReading? ParseNMEAGasData(string nmeaData)
    {
        // Custom NMEA parsing for gas data
        if (string.IsNullOrEmpty(nmeaData) || !nmeaData.StartsWith("$")) return null;
        
        var parts = nmeaData.Split(',');
        if (parts.Length < 4) return null;
        
        return new GasSensorReading
        {
            Timestamp = DateTime.UtcNow,
            SensorId = "NMEA_GAS",
            ChemicalName = parts[1],
            Concentration = double.Parse(parts[2]),
            ConcentrationUnit = parts[3],
            QualityFlag = "Good"
        };
    }

    private GasSensorReading? ParseGenericSerialData(string data, string portName)
    {
        try
        {
            var parts = data.Split(',');
            if (parts.Length < 2) return null;
            
            return new GasSensorReading
            {
                Timestamp = DateTime.UtcNow,
                SensorId = portName,
                ChemicalName = parts[0],
                Concentration = double.Parse(parts[1]),
                ConcentrationUnit = "ppm",
                QualityFlag = "Good"
            };
        }
        catch
        {
            return null;
        }
    }

    private IEnumerable<GasSensorReading> ParseCSVGasData(string csvData)
    {
        var readings = new List<GasSensorReading>();
        var lines = csvData.Split('\n');
        
        for (int i = 1; i < lines.Length; i++) // Skip header
        {
            var values = lines[i].Split(',');
            if (values.Length < 6) continue;
            
            readings.Add(new GasSensorReading
            {
                Timestamp = DateTime.Parse(values[0]),
                SensorId = values[1],
                ChemicalName = values[2],
                Concentration = double.Parse(values[3]),
                ConcentrationUnit = values[4],
                QualityFlag = values[5]
            });
        }
        
        return readings;
    }

    private IEnumerable<GasSensorReading> ParseJSONGasData(string jsonData)
    {
        try
        {
            var data = JsonDocument.Parse(jsonData);
            var readings = new List<GasSensorReading>();
            
            if (data.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in data.RootElement.EnumerateArray())
                {
                    readings.Add(new GasSensorReading
                    {
                        Timestamp = DateTime.UtcNow,
                        SensorId = item.GetProperty("sensorId").GetString() ?? "",
                        ChemicalName = item.GetProperty("chemical").GetString() ?? "",
                        Concentration = item.GetProperty("concentration").GetDouble(),
                        ConcentrationUnit = item.GetProperty("unit").GetString() ?? "ppm",
                        QualityFlag = "Good"
                    });
                }
            }
            
            return readings;
        }
        catch
        {
            return Enumerable.Empty<GasSensorReading>();
        }
    }

    private IEnumerable<GasSensorReading> ParseXMLGasData(string xmlData)
    {
        // Simplified XML parsing - in real implementation, use XDocument
        return Enumerable.Empty<GasSensorReading>();
    }

    private string ExtractChemicalFromNodeId(string nodeId)
    {
        // Extract chemical name from OPC UA node ID
        if (nodeId.Contains("NH3")) return "Ammonia";
        if (nodeId.Contains("SO2")) return "Sulfur Dioxide";
        if (nodeId.Contains("H2S")) return "Hydrogen Sulfide";
        return "Unknown";
    }

    private string ExtractChemicalFromTagName(string tagName)
    {
        // Extract chemical name from OSI PI tag name
        if (tagName.Contains("NH3")) return "Ammonia";
        if (tagName.Contains("SO2")) return "Sulfur Dioxide";
        if (tagName.Contains("H2S")) return "Hydrogen Sulfide";
        return "Unknown";
    }

    private string DetermineChemicalFromRegister(int register)
    {
        // Map Modbus registers to chemicals based on configuration
        return register switch
        {
            1000 => "Ammonia",
            1001 => "Sulfur Dioxide",
            1002 => "Hydrogen Sulfide",
            _ => "Unknown"
        };
    }

    private int[] ParseIntArray(string value)
    {
        return value.Split(',').Select(int.Parse).ToArray();
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        _httpClient?.Dispose();
    }
}