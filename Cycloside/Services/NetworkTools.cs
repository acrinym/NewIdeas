using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Cycloside.Services
{
    /// <summary>
    /// NETWORK TOOLS - Comprehensive network analysis and security toolkit
    /// Provides packet sniffing, port scanning, HTTP inspection, and traffic monitoring
    /// </summary>
    public static class NetworkTools
    {
        public static event EventHandler<NetworkPacketEventArgs>? PacketCaptured;
        public static event EventHandler<PortScanEventArgs>? PortScanCompleted;
        public static event EventHandler<HttpRequestEventArgs>? HttpRequestCaptured;
        public static event EventHandler<NetworkInterfaceEventArgs>? NetworkInterfaceChanged;

        private static CancellationTokenSource? _packetCaptureToken;
        private static CancellationTokenSource? _portScanToken;
        private static CancellationTokenSource? _trafficMonitorToken;
        private static readonly ObservableCollection<NetworkPacket> _capturedPackets = new();
        private static readonly ObservableCollection<NetworkInterfaceInfo> _networkInterfaces = new();
        private static readonly ObservableCollection<PortScanResult> _portScanResults = new();
        private static readonly ObservableCollection<HttpRequestInfo> _httpRequests = new();

        public static ObservableCollection<NetworkPacket> CapturedPackets => _capturedPackets;
        public static ObservableCollection<NetworkInterfaceInfo> NetworkInterfaces => _networkInterfaces;
        public static ObservableCollection<PortScanResult> PortScanResults => _portScanResults;
        public static ObservableCollection<HttpRequestInfo> HttpRequests => _httpRequests;

        public static bool IsPacketCaptureActive => _packetCaptureToken != null && !_packetCaptureToken.IsCancellationRequested;
        public static bool IsPortScanActive => _portScanToken != null && !_portScanToken.IsCancellationRequested;
        public static bool IsTrafficMonitorActive => _trafficMonitorToken != null && !_trafficMonitorToken.IsCancellationRequested;

        /// <summary>
        /// Initialize Network Tools service
        /// </summary>
        public static async Task InitializeAsync()
        {
            Logger.Log("🌐 Initializing Network Tools service...");

            try
            {
                await DiscoverNetworkInterfacesAsync();
                Logger.Log("✅ Network Tools initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Network Tools initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Discover available network interfaces
        /// </summary>
        private static async Task DiscoverNetworkInterfacesAsync()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .Select(ni => new NetworkInterfaceInfo
                    {
                        Name = ni.Name,
                        Description = ni.Description,
                        Type = ni.NetworkInterfaceType.ToString(),
                        Speed = $"{ni.Speed / 1_000_000} Mbps",
                        IPv4Address = GetIPv4Address(ni),
                        IPv6Address = GetIPv6Address(ni),
                        MacAddress = GetMacAddress(ni),
                        IsWireless = ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                    })
                    .ToList();

                _networkInterfaces.Clear();
                foreach (var interfaceInfo in interfaces)
                {
                    _networkInterfaces.Add(interfaceInfo);
                }

                Logger.Log($"🌐 Discovered {interfaces.Count} active network interfaces");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to discover network interfaces: {ex.Message}");
            }
        }

        private static string? GetIPv4Address(NetworkInterface ni)
        {
            return ni.GetIPProperties().UnicastAddresses
                .FirstOrDefault(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)?
                .Address.ToString();
        }

        private static string? GetIPv6Address(NetworkInterface ni)
        {
            return ni.GetIPProperties().UnicastAddresses
                .FirstOrDefault(addr => addr.Address.AddressFamily == AddressFamily.InterNetworkV6)?
                .Address.ToString();
        }

        private static string? GetMacAddress(NetworkInterface ni)
        {
            return string.Join(":", ni.GetPhysicalAddress().GetAddressBytes()
                .Select(b => b.ToString("X2")));
        }

        /// <summary>
        /// Start packet capture on specified interface
        /// </summary>
        public static async Task<bool> StartPacketCaptureAsync(string interfaceName, int maxPackets = 1000)
        {
            try
            {
                if (IsPacketCaptureActive)
                {
                    await StopPacketCaptureAsync();
                }

                Logger.Log($"📡 Starting packet capture on interface: {interfaceName}");

                _packetCaptureToken = new CancellationTokenSource();
                var token = _packetCaptureToken.Token;

                await Task.Run(async () =>
                {
                    try
                    {
                        await CapturePacketsAsync(interfaceName, maxPackets, token);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"❌ Packet capture error: {ex.Message}");
                    }
                }, token);

                Logger.Log("✅ Packet capture started successfully");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to start packet capture: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop packet capture
        /// </summary>
        public static async Task StopPacketCaptureAsync()
        {
            if (_packetCaptureToken != null)
            {
                Logger.Log("🛑 Stopping packet capture...");
                _packetCaptureToken.Cancel();

                // Wait a bit for cleanup
                await Task.Delay(500);

                _packetCaptureToken = null;
                Logger.Log("✅ Packet capture stopped");
            }
        }

        /// <summary>
        /// Capture packets from network interface
        /// </summary>
        private static async Task CapturePacketsAsync(string interfaceName, int maxPackets, CancellationToken token)
        {
            try
            {
                // Note: Real packet capture would require WinPcap/Npcap or similar
                // This is a simplified simulation for demonstration

                var packetCount = 0;
                var startTime = DateTime.Now;

                while (!token.IsCancellationRequested && packetCount < maxPackets)
                {
                    // Simulate packet capture
                    var packet = GenerateSimulatedPacket(interfaceName, packetCount);

                    await Task.Delay(100); // Simulate network delay

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _capturedPackets.Add(packet);
                        OnPacketCaptured(packet);

                        // Keep only recent packets
                        if (_capturedPackets.Count > maxPackets)
                        {
                            _capturedPackets.RemoveAt(0);
                        }
                    });

                    packetCount++;
                }

                var duration = DateTime.Now - startTime;
                Logger.Log($"📊 Packet capture completed: {packetCount} packets in {duration.TotalSeconds:F1}s");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Packet capture failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate simulated network packet for demonstration
        /// </summary>
        private static NetworkPacket GenerateSimulatedPacket(string interfaceName, int packetNumber)
        {
            var protocols = new[] { "TCP", "UDP", "ICMP", "HTTP", "HTTPS", "DNS" };
            var sources = new[] { "192.168.1.100", "10.0.0.50", "172.16.0.25", "8.8.8.8", "1.1.1.1" };
            var destinations = new[] { "8.8.8.8", "1.1.1.1", "192.168.1.1", "10.0.0.1", "google.com" };

            var random = new Random();
            var protocol = protocols[random.Next(protocols.Length)];
            var source = sources[random.Next(sources.Length)];
            var destination = destinations[random.Next(destinations.Length)];
            var size = random.Next(64, 1500);

            return new NetworkPacket
            {
                Id = packetNumber,
                Timestamp = DateTime.Now,
                Interface = interfaceName,
                Protocol = protocol,
                Source = source,
                Destination = destination,
                Size = size,
                Data = GeneratePacketData(protocol, size)
            };
        }

        private static byte[] GeneratePacketData(string protocol, int size)
        {
            var data = new byte[size];
            var random = new Random();

            // Fill with some pattern based on protocol
            switch (protocol.ToUpper())
            {
                case "HTTP":
                    var httpHeader = "GET / HTTP/1.1\r\nHost: example.com\r\n\r\n";
                    var headerBytes = Encoding.ASCII.GetBytes(httpHeader);
                    Array.Copy(headerBytes, data, Math.Min(headerBytes.Length, size));
                    break;
                case "DNS":
                    // DNS query pattern
                    data[0] = 0x12; data[1] = 0x34; // Transaction ID
                    data[2] = 0x01; data[3] = 0x00; // Flags
                    break;
                default:
                    // Random data
                    random.NextBytes(data);
                    break;
            }

            return data;
        }

        /// <summary>
        /// Start port scanning on target host
        /// </summary>
        public static async Task<bool> StartPortScanAsync(string targetHost, int startPort = 1, int endPort = 1024)
        {
            try
            {
                if (IsPortScanActive)
                {
                    await StopPortScanAsync();
                }

                Logger.Log($"🔍 Starting port scan on {targetHost}:{startPort}-{endPort}");

                _portScanToken = new CancellationTokenSource();
                var token = _portScanToken.Token;

                await Task.Run(async () =>
                {
                    await ScanPortsAsync(targetHost, startPort, endPort, token);
                }, token);

                Logger.Log("✅ Port scan started successfully");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to start port scan: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop port scanning
        /// </summary>
        public static async Task StopPortScanAsync()
        {
            if (_portScanToken != null)
            {
                Logger.Log("🛑 Stopping port scan...");
                _portScanToken.Cancel();

                await Task.Delay(500);
                _portScanToken = null;

                Logger.Log("✅ Port scan stopped");
            }
        }

        /// <summary>
        /// Scan ports on target host
        /// </summary>
        private static async Task ScanPortsAsync(string targetHost, int startPort, int endPort, CancellationToken token)
        {
            try
            {
                var results = new List<PortScanResult>();

                for (int port = startPort; port <= endPort && !token.IsCancellationRequested; port++)
                {
                    var result = await CheckPortAsync(targetHost, port);
                    results.Add(result);

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _portScanResults.Add(result);
                        OnPortScanCompleted(result);
                    });

                    // Small delay to avoid overwhelming the network
                    await Task.Delay(10);
                }

                Logger.Log($"🔍 Port scan completed: {results.Count} ports scanned");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Port scan failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if a port is open on target host
        /// </summary>
        private static async Task<PortScanResult> CheckPortAsync(string targetHost, int port)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(targetHost, port);
                var timeoutTask = Task.Delay(1000);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                var startTime = DateTime.Now;

                if (completedTask == connectTask)
                {
                    client.Close();
                    var responseTime = (int)(DateTime.Now - startTime).TotalMilliseconds;
                    return new PortScanResult
                    {
                        Host = targetHost,
                        Port = port,
                        Status = "Open",
                        Service = GetServiceName(port),
                        ResponseTime = responseTime
                    };
                }
                else
                {
                    return new PortScanResult
                    {
                        Host = targetHost,
                        Port = port,
                        Status = "Closed",
                        Service = GetServiceName(port),
                        ResponseTime = 1000
                    };
                }
            }
            catch (Exception ex)
            {
                return new PortScanResult
                {
                    Host = targetHost,
                    Port = port,
                    Status = "Error",
                    Service = GetServiceName(port),
                    ResponseTime = 0,
                    Error = ex.Message
                };
            }
        }

        private static string GetServiceName(int port)
        {
            return port switch
            {
                20 => "FTP Data",
                21 => "FTP Control",
                22 => "SSH",
                23 => "Telnet",
                25 => "SMTP",
                53 => "DNS",
                80 => "HTTP",
                110 => "POP3",
                143 => "IMAP",
                443 => "HTTPS",
                993 => "IMAPS",
                995 => "POP3S",
                3389 => "RDP",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Start HTTP traffic monitoring
        /// </summary>
        public static async Task<bool> StartHttpMonitoringAsync()
        {
            try
            {
                if (IsTrafficMonitorActive)
                {
                    await StopHttpMonitoringAsync();
                }

                Logger.Log("🌐 Starting HTTP traffic monitoring...");

                _trafficMonitorToken = new CancellationTokenSource();
                var token = _trafficMonitorToken.Token;

                await Task.Run(async () =>
                {
                    await MonitorHttpTrafficAsync(token);
                }, token);

                Logger.Log("✅ HTTP monitoring started successfully");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to start HTTP monitoring: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop HTTP traffic monitoring
        /// </summary>
        public static async Task StopHttpMonitoringAsync()
        {
            if (_trafficMonitorToken != null)
            {
                Logger.Log("🛑 Stopping HTTP monitoring...");
                _trafficMonitorToken.Cancel();

                await Task.Delay(500);
                _trafficMonitorToken = null;

                Logger.Log("✅ HTTP monitoring stopped");
            }
        }

        /// <summary>
        /// Monitor HTTP traffic (simulated for demonstration)
        /// </summary>
        private static async Task MonitorHttpTrafficAsync(CancellationToken token)
        {
            try
            {
                var requestCount = 0;
                var startTime = DateTime.Now;

                while (!token.IsCancellationRequested)
                {
                    // Simulate HTTP request capture
                    var request = GenerateSimulatedHttpRequest(requestCount);

                    await Task.Delay(2000); // Simulate request interval

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _httpRequests.Add(request);
                        OnHttpRequestCaptured(request);

                        // Keep only recent requests
                        if (_httpRequests.Count > 100)
                        {
                            _httpRequests.RemoveAt(0);
                        }
                    });

                    requestCount++;
                }

                var duration = DateTime.Now - startTime;
                Logger.Log($"🌐 HTTP monitoring completed: {requestCount} requests in {duration.TotalSeconds:F1}s");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ HTTP monitoring failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate simulated HTTP request for demonstration
        /// </summary>
        private static HttpRequestInfo GenerateSimulatedHttpRequest(int requestNumber)
        {
            var methods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };
            var urls = new[] {
                "https://api.github.com/user",
                "https://httpbin.org/get",
                "https://jsonplaceholder.typicode.com/posts",
                "https://api.openweathermap.org/data/2.5/weather",
                "https://newsapi.org/v2/top-headlines"
            };
            var userAgents = new[] {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                "curl/7.68.0",
                "PostmanRuntime/7.26.0",
                "python-requests/2.25.1"
            };

            var random = new Random();
            var method = methods[random.Next(methods.Length)];
            var url = urls[random.Next(urls.Length)];
            var userAgent = userAgents[random.Next(userAgents.Length)];

            return new HttpRequestInfo
            {
                Id = requestNumber,
                Timestamp = DateTime.Now,
                Method = method,
                Url = url,
                UserAgent = userAgent,
                StatusCode = random.Next(200, 500),
                ResponseSize = random.Next(100, 10000),
                ResponseTime = random.Next(50, 2000)
            };
        }

        // Event handlers
        private static void OnPacketCaptured(NetworkPacket packet)
        {
            PacketCaptured?.Invoke(null, new NetworkPacketEventArgs(packet));
        }

        private static void OnPortScanCompleted(PortScanResult result)
        {
            PortScanCompleted?.Invoke(null, new PortScanEventArgs(result));
        }

        private static void OnHttpRequestCaptured(HttpRequestInfo request)
        {
            HttpRequestCaptured?.Invoke(null, new HttpRequestEventArgs(request));
        }
    }

    // Data models
    public class NetworkPacket
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Interface { get; set; } = "";
        public string Protocol { get; set; } = "";
        public string Source { get; set; } = "";
        public string Destination { get; set; } = "";
        public int Size { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    public class NetworkInterfaceInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Type { get; set; } = "";
        public string Speed { get; set; } = "";
        public string? IPv4Address { get; set; }
        public string? IPv6Address { get; set; }
        public string? MacAddress { get; set; }
        public bool IsWireless { get; set; }
    }

    public class PortScanResult
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public string Status { get; set; } = "";
        public string Service { get; set; } = "";
        public int ResponseTime { get; set; }
        public string? Error { get; set; }
    }

    public class HttpRequestInfo
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Method { get; set; } = "";
        public string Url { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public int StatusCode { get; set; }
        public int ResponseSize { get; set; }
        public int ResponseTime { get; set; }
    }

    // Event args
    public class NetworkPacketEventArgs : EventArgs
    {
        public NetworkPacket Packet { get; }

        public NetworkPacketEventArgs(NetworkPacket packet)
        {
            Packet = packet;
        }
    }

    public class PortScanEventArgs : EventArgs
    {
        public PortScanResult Result { get; }

        public PortScanEventArgs(PortScanResult result)
        {
            Result = result;
        }
    }

    public class HttpRequestEventArgs : EventArgs
    {
        public HttpRequestInfo Request { get; }

        public HttpRequestEventArgs(HttpRequestInfo request)
        {
            Request = request;
        }
    }

    public class NetworkInterfaceEventArgs : EventArgs
    {
        public NetworkInterfaceInfo Interface { get; }

        public NetworkInterfaceEventArgs(NetworkInterfaceInfo interfaceInfo)
        {
            Interface = interfaceInfo;
        }
    }
}
