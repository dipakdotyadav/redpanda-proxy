using System;
using System.Net;
using System.Net.Sockets;
namespace RedpandaProxy
{

    public class BrokerConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 9092;
        public int Weight { get; set; } = 1;
        public bool Enabled { get; set; } = true;
    }

    // Broker connection management
    public class BrokerConnection
    {
        public BrokerConfig Config { get; set; }
        public TcpClient? TcpClient { get; set; }
        public NetworkStream? Stream { get; set; }
        public bool IsConnected { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public int ActiveConnections { get; set; }
        public readonly object Lock = new();

        public BrokerConnection(BrokerConfig config)
        {
            Config = config;
            IsConnected = false;
            LastHealthCheck = DateTime.MinValue;
            ActiveConnections = 0;
        }
    }
}