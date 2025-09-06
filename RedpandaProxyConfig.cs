namespace RedpandaProxy
{
    public enum LoadBalancingStrategy
    {
        RoundRobin,
        Random,
        Weighted,
        LeastConnections
    }
        // Configuration models
    public class RedpandaProxyConfig
    {
        public int ListenPort { get; set; } = 9092;
        public string ListenAddress { get; set; } = "0.0.0.0";
        public List<BrokerConfig> Brokers { get; set; } = new();
        public LoadBalancingStrategy LoadBalancing { get; set; } = LoadBalancingStrategy.RoundRobin;
        public int ConnectionTimeout { get; set; } = 30000;
        public int HealthCheckInterval { get; set; } = 10000;
    }
}