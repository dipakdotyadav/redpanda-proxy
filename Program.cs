using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RedpandaProxy
{
    // Program entry point and configuration
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            var services = new ServiceCollection();
            
            // Configure logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddConfiguration(configuration.GetSection("Logging"));
            });

            // Load configuration
            var config = new RedpandaProxyConfig();
            configuration.GetSection("RedpandaProxy").Bind(config);
            
            // Validate configuration
            if (!config.Brokers.Any())
            {
                Console.WriteLine("Error: No brokers configured. Please check your configuration.");
                return;
            }

            services.AddSingleton(config);

            // Register load balancer based on strategy
            services.AddSingleton<ILoadBalancer>(provider =>
            {
                return config.LoadBalancing switch
                {
                    LoadBalancingStrategy.RoundRobin => new RoundRobinLoadBalancer(),
                    LoadBalancingStrategy.Random => new RandomLoadBalancer(),
                    LoadBalancingStrategy.Weighted => new WeightedLoadBalancer(),
                    LoadBalancingStrategy.LeastConnections => new LeastConnectionsLoadBalancer(),
                    _ => new RoundRobinLoadBalancer()
                };
            });

            services.AddSingleton<BrokerPool>();
            services.AddSingleton<ClientConnectionHandler>();
            services.AddHostedService<RedpandaProxyService>();

            // Build and run
            var serviceProvider = services.BuildServiceProvider();
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(s => s.AddSingleton(serviceProvider))
                .UseConsoleLifetime()
                .Build();

            // Display startup information
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting Redpanda Proxy...");
            logger.LogInformation($"Listen Address: {config.ListenAddress}:{config.ListenPort}");
            logger.LogInformation($"Load Balancing: {config.LoadBalancing}");
            
            foreach (var broker in config.Brokers.Where(b => b.Enabled))
            {
                logger.LogInformation($"Broker: {broker.Host}:{broker.Port} (Weight: {broker.Weight})");
            }

            try
            {
                await serviceProvider.GetRequiredService<RedpandaProxyService>().StartAsync(CancellationToken.None);
                
                Console.WriteLine("Redpanda Proxy is running. Press Ctrl+C to stop.");
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    logger.LogInformation("Shutdown requested...");
                };

                // Keep running until cancelled
                var tcs = new TaskCompletionSource<bool>();
                Console.CancelKeyPress += (_, _) => tcs.SetResult(true);
                await tcs.Task;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fatal error occurred");
            }
            finally
            {
                await serviceProvider.GetRequiredService<RedpandaProxyService>().StopAsync(CancellationToken.None);
                serviceProvider.Dispose();
            }
        }
    }
}