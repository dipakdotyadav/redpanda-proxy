using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
namespace RedpandaProxy
{
    // Client connection handler
    public class ClientConnectionHandler
    {
        private readonly BrokerPool _brokerPool;
        private readonly ILogger<ClientConnectionHandler> _logger;
        private readonly RedpandaProxyConfig _config;

        public ClientConnectionHandler(BrokerPool brokerPool, RedpandaProxyConfig config, ILogger<ClientConnectionHandler> logger)
        {
            _brokerPool = brokerPool;
            _config = config;
            _logger = logger;
        }

        public async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
            _logger.LogInformation($"New client connection from {clientEndpoint}");

            BrokerConnection? broker = null;
            try
            {
                broker = await _brokerPool.GetBrokerAsync();
                if (broker == null)
                {
                    _logger.LogError("No available brokers for client connection");
                    return;
                }

                _brokerPool.IncrementConnectionCount(broker);
                _logger.LogInformation($"Routing client {clientEndpoint} to broker {broker.Config.Host}:{broker.Config.Port}");

                using var clientStream = client.GetStream();
                using var brokerClient = new TcpClient();

                await brokerClient.ConnectAsync(broker.Config.Host, broker.Config.Port);
                using var brokerStream = brokerClient.GetStream();

                // Start bidirectional data forwarding
                var forwardTasks = new[]
                {
                    ForwardDataAsync(clientStream, brokerStream, "Client->Broker", cancellationToken),
                    ForwardDataAsync(brokerStream, clientStream, "Broker->Client", cancellationToken)
                };

                await Task.WhenAny(forwardTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling client connection from {clientEndpoint}");
            }
            finally
            {
                if (broker != null)
                {
                    _brokerPool.DecrementConnectionCount(broker);
                }

                client.Close();
                _logger.LogInformation($"Client connection from {clientEndpoint} closed");
            }
        }

        private async Task ForwardDataAsync(NetworkStream source, NetworkStream destination, string direction, CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0) break;

                    await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    await destination.FlushAsync(cancellationToken);

                    _logger.LogTrace($"{direction}: Forwarded {bytesRead} bytes");
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                _logger.LogError(ex, $"Error in data forwarding ({direction})");
            }
        }
    }

}