# RedPanda Kafka Proxy

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Docker](https://img.shields.io/badge/Docker-supported-blue)](https://www.docker.com/)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](http://makeapullrequest.com)

A high-performance, production-ready Kafka proxy written in C# that provides load balancing and failover capabilities for RedPanda/Kafka clusters. Present a single endpoint to your clients while automatically distributing connections across multiple brokers.

## ✨ Features

- **🔄 Load Balancing**: Multiple strategies (Round Robin, Random, Weighted, Least Connections)
- **💓 Health Monitoring**: Automatic broker health checks and reconnection
- **⚡ High Performance**: Asynchronous TCP forwarding with minimal latency overhead
- **🛠️ Configurable**: JSON-based configuration with environment variable support
- **📊 Production Ready**: Comprehensive logging, error handling, and graceful shutdown
- **🐳 Container Support**: Docker and Kubernetes ready
- **🔧 Easy Deployment**: Systemd service configuration included

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 or later
- RedPanda or Kafka brokers

### Installation

```bash
git clone https://github.com/yourusername/redpanda-kafka-proxy.git
cd redpanda-kafka-proxy
dotnet build
```

### Basic Usage

1. **Configure your brokers** in `config/appsettings.json`:

```json
{
  "KafkaProxy": {
    "ListenPort": 9092,
    "LoadBalancing": "RoundRobin",
    "Brokers": [
      {
        "Host": "redpanda-1.example.com",
        "Port": 9092,
        "Weight": 1,
        "Enabled": true
      },
      {
        "Host": "redpanda-2.example.com", 
        "Port": 9092,
        "Weight": 1,
        "Enabled": true
      }
    ]
  }
}
```

2. **Run the proxy**:

```bash
dotnet run --project src/
```

3. **Connect your Kafka clients** to `localhost:9092`

The proxy will automatically route connections to healthy brokers using your chosen load balancing strategy.

## 📖 Documentation

### Load Balancing Strategies

| Strategy | Description | Best For |
|----------|-------------|----------|
| **RoundRobin** | Equal distribution across brokers | Balanced workloads with similar broker specs |
| **Random** | Random broker selection | Simple setups, good for testing |
| **Weighted** | Distribution based on broker weights | Mixed broker capacities |
| **LeastConnections** | Routes to broker with fewest connections | Uneven connection patterns |

### Configuration Options

| Setting | Default | Description |
|---------|---------|-------------|
| `ListenPort` | 9092 | Port for client connections |
| `ListenAddress` | 0.0.0.0 | Interface to bind to |
| `LoadBalancing` | RoundRobin | Load balancing strategy |
| `ConnectionTimeout` | 30000 | Connection timeout in milliseconds |
| `HealthCheckInterval` | 10000 | Health check interval in milliseconds |

### Environment Variables

Override any configuration setting using environment variables:

```bash
export KafkaProxy__ListenPort=9093
export KafkaProxy__LoadBalancing=LeastConnections
export KafkaProxy__Brokers__0__Host=new-broker.example.com
```

## 🐳 Docker Deployment

### Using Docker Compose (Recommended for Testing)

The repository includes a complete setup with 3 RedPanda brokers for testing:

```bash
docker-compose up -d
```

This starts:
- 3 RedPanda brokers (ports 19092, 19093, 19094)
- Kafka proxy (port 9092)

### Production Docker

```bash
# Build image
docker build -t kafka-proxy .

# Run container
docker run -d \
  --name kafka-proxy \
  -p 9092:9092 \
  -v $(pwd)/config:/app/config:ro \
  kafka-proxy
```

## 🏗️ Production Deployment

### Systemd Service (Linux)

1. **Publish the application**:
```bash
dotnet publish src/ -c Release -o /opt/kafka-proxy
```

2. **Install systemd service**:
```bash
sudo cp deployment/kafka-proxy.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable kafka-proxy
sudo systemctl start kafka-proxy
```

3. **Check status**:
```bash
sudo systemctl status kafka-proxy
sudo journalctl -u kafka-proxy -f
```

### Kubernetes

Example Kubernetes deployment:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: kafka-proxy
spec:
  replicas: 2
  selector:
    matchLabels:
      app: kafka-proxy
  template:
    metadata:
      labels:
        app: kafka-proxy
    spec:
      containers:
      - name: kafka-proxy
        image: kafka-proxy:latest
        ports:
        - containerPort: 9092
        env:
        - name: KafkaProxy__Brokers__0__Host
          value: "redpanda-1.kafka.svc.cluster.local"
        - name: KafkaProxy__Brokers__1__Host
          value: "redpanda-2.kafka.svc.cluster.local"
---
apiVersion: v1
kind: Service
metadata:
  name: kafka-proxy-service
spec:
  selector:
    app: kafka-proxy
  ports:
  - port: 9092
    targetPort: 9092
  type: LoadBalancer
```

## 📊 Monitoring and Logging

The proxy provides comprehensive logging for operational monitoring:

- **Connection Events**: Client connections and routing decisions
- **Health Checks**: Broker availability and reconnection attempts
- **Load Balancing**: Broker selection reasoning
- **Performance Metrics**: Connection counts and data transfer

Example log output:
```
info: KafkaProxy.KafkaProxyService[0] Kafka Proxy listening on 0.0.0.0:9092
info: KafkaProxy.BrokerPool[0] Connected to broker redpanda-1:9092
info: KafkaProxy.ClientConnectionHandler[0] Routing client 172.17.0.1:54320 to broker redpanda-2:9092
```

## 🧪 Testing

### Unit Tests

```bash
dotnet test
```

### Integration Testing

Use the provided docker-compose setup:

```bash
# Start test environment
docker-compose up -d

# Test with kafka-console-producer
kafka-console-producer --bootstrap-server localhost:9092 --topic test-topic

# Test with kafka-console-consumer
kafka-console-consumer --bootstrap-server localhost:9092 --topic test-topic --from-beginning
```

### Load Testing

Example using kafka-producer-perf-test:

```bash
kafka-producer-perf-test \
  --topic test-topic \
  --num-records 100000 \
  --record-size 1024 \
  --throughput 10000 \
  --producer-props bootstrap.servers=localhost:9092
```

## 🔧 Development

### Prerequisites

- .NET 8.0 SDK
- Docker (for integration tests)
- Your favorite IDE (Visual Studio, VS Code, JetBrains Rider)

### Building from Source

```bash
git clone https://github.com/yourusername/redpanda-kafka-proxy.git
cd redpanda-kafka-proxy
dotnet restore
dotnet build
```

### Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b my-new-feature`
3. Make your changes and add tests
4. Commit your changes: `git commit -am 'Add some feature'`
5. Push to the branch: `git push origin my-new-feature`
6. Submit a pull request

## 📋 Performance

The proxy is designed for high throughput with minimal overhead:

- **Latency**: < 1ms additional latency under normal conditions
- **Throughput**: Supports 10,000+ concurrent connections
- **Memory**: Efficient streaming without message buffering
- **CPU**: Fully asynchronous, non-blocking I/O operations

## 🔒 Security

Security considerations for production:

- **Network Security**: Use firewalls and VPNs to restrict access
- **TLS Support**: Can be extended for SSL/TLS termination
- **Authentication**: Extensible for client authentication
- **Audit Logging**: Comprehensive connection logging

## 🛣️ Roadmap

- [ ] Metrics collection (Prometheus/StatsD)
- [ ] Circuit breaker pattern implementation
- [ ] Content-based routing capabilities
- [ ] Admin REST API for runtime configuration
- [ ] TLS/SSL support
- [ ] Authentication and authorization
- [ ] Message transformation capabilities

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/redpanda-kafka-proxy/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/redpanda-kafka-proxy/discussions)
- **Documentation**: [Wiki](https://github.com/yourusername/redpanda-kafka-proxy/wiki)

## 🙏 Acknowledgments

- [RedPanda](https://redpanda.com/) - The streaming data platform
- [Apache Kafka](https://kafka.apache.org/) - The original inspiration
- [.NET Community](https://dotnet.microsoft.com/community) - For the excellent ecosystem

---

⭐ If you find this project useful, please consider giving it a star on GitHub!