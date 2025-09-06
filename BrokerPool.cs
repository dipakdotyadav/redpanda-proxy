using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
namespace RedpandaProxy
{
    // Broker pool manager
    public class BrokerPool
    {
        private readonly List<BrokerConnection> _brokers;
        private readonly ILoadBalancer _loadBalancer;
        private readonly ILogger<BrokerPool> _logger;
        private readonly RedpandaProxyConfig _config;
        private readonly Timer _healthCheckTimer;

        public BrokerPool(RedpandaProxyConfig config, ILoadBalancer loadBalancer, ILogger<BrokerPool> logger)
        {
            _config = config;
            _loadBalancer = loadBalancer;
            _logger = logger;
            _brokers = config.Brokers.Select(b => new BrokerConnection(b)).ToList();

            _healthCheckTimer = new Timer(PerformHealthChecks, null,
                TimeSpan.FromMilliseconds(config.HealthCheckInterval),
                TimeSpan.FromMilliseconds(config.HealthCheckInterval));
        }

        public async Task<BrokerConnection?> GetBrokerAsync()
        {
            var availableBrokers = _brokers.Where(b => b.IsConnected && b.Config.Enabled).ToList();

            if (!availableBrokers.Any())
            {
                _logger.LogWarning("No available brokers found, attempting to reconnect...");
                await ReconnectBrokersAsync();
                availableBrokers = _brokers.Where(b => b.IsConnected && b.Config.Enabled).ToList();
            }

            return _loadBalancer.SelectBroker(availableBrokers);
        }

        private async Task ReconnectBrokersAsync()
        {
            var reconnectTasks = _brokers
                .Where(b => !b.IsConnected && b.Config.Enabled)
                .Select(ConnectToBrokerAsync);

            await Task.WhenAll(reconnectTasks);
        }

        private async Task ConnectToBrokerAsync(BrokerConnection broker)
        {
            try
            {
                lock (broker.Lock)
                {
                    broker.TcpClient?.Close();
                    broker.TcpClient = new TcpClient();
                }

                await broker.TcpClient.ConnectAsync(broker.Config.Host, broker.Config.Port);

                lock (broker.Lock)
                {
                    broker.Stream = broker.TcpClient.GetStream();
                    broker.IsConnected = true;
                    broker.LastHealthCheck = DateTime.UtcNow;
                }

                _logger.LogInformation($"Connected to broker {broker.Config.Host}:{broker.Config.Port}");
            }
            catch (Exception ex)
            {
                lock (broker.Lock)
                {
                    broker.IsConnected = false;
                    broker.TcpClient?.Close();
                    broker.TcpClient = null;
                    broker.Stream = null;
                }

                _logger.LogError(ex, $"Failed to connect to broker {broker.Config.Host}:{broker.Config.Port}");
            }
        }

        private async void PerformHealthChecks(object? state)
        {
            var healthCheckTasks = _brokers.Select(PerformHealthCheckAsync);
            await Task.WhenAll(healthCheckTasks);
        }

        private async Task PerformHealthCheckAsync(BrokerConnection broker)
        {
            if (!broker.Config.Enabled) return;

            try
            {
                if (!broker.IsConnected || broker.TcpClient?.Connected != true)
                {
                    await ConnectToBrokerAsync(broker);
                    return;
                }

                // Simple connectivity check
                lock (broker.Lock)
                {
                    if (broker.Stream?.CanRead == true && broker.Stream?.CanWrite == true)
                    {
                        broker.LastHealthCheck = DateTime.UtcNow;
                    }
                    else
                    {
                        broker.IsConnected = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Health check failed for broker {broker.Config.Host}:{broker.Config.Port}");
                lock (broker.Lock)
                {
                    broker.IsConnected = false;
                }
            }
        }

        public void IncrementConnectionCount(BrokerConnection broker)
        {
            var _activeConnection = broker.ActiveConnections;
            Interlocked.Increment(ref _activeConnection);
            broker.ActiveConnections = _activeConnection;
        }

        public void DecrementConnectionCount(BrokerConnection broker)
        {
            var _activeConnection = broker.ActiveConnections;
            Interlocked.Decrement(ref _activeConnection);
            broker.ActiveConnections = _activeConnection;
        }
    }

}