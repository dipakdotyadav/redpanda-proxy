using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
namespace RedpandaProxy
{
    // Main proxy service
    public class RedpandaProxyService : BackgroundService
    {
        private readonly RedpandaProxyConfig _config;
        private readonly BrokerPool _brokerPool;
        private readonly ClientConnectionHandler _clientHandler;
        private readonly ILogger<RedpandaProxyService> _logger;
        private TcpListener? _tcpListener;

        public RedpandaProxyService(
            RedpandaProxyConfig config,
            BrokerPool brokerPool,
            ClientConnectionHandler clientHandler,
            ILogger<RedpandaProxyService> logger)
        {
            _config = config;
            _brokerPool = brokerPool;
            _clientHandler = clientHandler;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var listenAddress = IPAddress.Parse(_config.ListenAddress);
            _tcpListener = new TcpListener(listenAddress, _config.ListenPort);

            try
            {
                _tcpListener.Start();
                _logger.LogInformation($"Redpanda Proxy listening on {_config.ListenAddress}:{_config.ListenPort}");
                _logger.LogInformation($"Configured {_config.Brokers.Count} broker(s) with {_config.LoadBalancing} load balancing");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var client = await _tcpListener.AcceptTcpClientAsync();

                        // Handle client connection in background
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _clientHandler.HandleClientAsync(client, stoppingToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Unhandled exception in client handler");
                            }
                        }, stoppingToken);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Expected when stopping
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error accepting client connection");
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Redpanda Proxy service");
            }
            finally
            {
                _tcpListener?.Stop();
                _logger.LogInformation("Redpanda Proxy service stopped");
            }
        }
    }

}