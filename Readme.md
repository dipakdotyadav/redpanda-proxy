

# Redpanda Proxy for RedPanda

A high-performance, configurable Redpanda proxy written in C# that can load balance connections across multiple RedPanda brokers while presenting a single endpoint to Redpanda clients.

## Features

- **Multiple Load Balancing Strategies**: Round Robin, Random, Weighted, Least Connections
- **Health Monitoring**: Automatic broker health checks and reconnection
- **High Performance**: Asynchronous TCP forwarding with minimal overhead  
- **Configurable**: JSON-based configuration with environment variable support
- **Production Ready**: Comprehensive logging, error handling, and graceful shutdown
- **Container Support**: Docker and Kubernetes ready

## Quick Start

1. Configure your brokers in `appsettings.json`
2. Run with `dotnet run`
3. Clients connect to the proxy on port 9092 (default)
4. Proxy routes traffic to healthy RedPanda brokers

## Configuration

Key configuration options:
- `ListenPort`: Port for client connections (default: 9092)
- `LoadBalancing`: Strategy - RoundRobin, Random, Weighted, LeastConnections
- `Brokers`: Array of broker configurations with host, port, weight, enabled status
- `HealthCheckInterval`: Milliseconds between health checks (default: 10000)

## Load Balancing Strategies

- **Round Robin**: Equal distribution across brokers
- **Random**: Random broker selection
- **Weighted**: Distribution based on broker weights
- **Least Connections**: Route to broker with fewest active connections

## Production Deployment

- Use systemd service for Linux deployments
- Configure appropriate logging levels
- Set up monitoring and alerts
- Consider running multiple proxy instances behind a load balancer

## Testing

Use the included docker-compose.yml to test with 3 RedPanda instances:
```bash
docker-compose up -d
```

Then test with any Kafka/Redpanda client connecting to localhost:9092.

-----

# Publish release build
```
dotnet publish -c Release -o ./publish
```

# Copy to server and set up systemd service
```
sudo cp kafka-proxy.service /etc/systemd/system/
sudo systemctl enable kafka-proxy
sudo systemctl start kafka-proxy
```

---------------
# Test with kafka-console-producer
```
kafka-console-producer --bootstrap-server localhost:9092 --topic test-topic
```

# Test with kafka-console-consumer  
```
kafka-console-consumer --bootstrap-server localhost:9092 --topic test-topic --from-beginning
```

------------------
# Override specific settings
```
export KafkaProxy__ListenPort=9093
export KafkaProxy__LoadBalancing=LeastConnections
export KafkaProxy__Brokers__0__Host=new-broker.example.com
```

----------------------
```
dotnet run --KafkaProxy:ListenPort=9093 --KafkaProxy:LoadBalancing=Weighted
```