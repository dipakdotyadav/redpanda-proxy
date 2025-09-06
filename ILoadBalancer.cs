namespace RedpandaProxy
{
    // Load balancer interface
    public interface ILoadBalancer
    {
        BrokerConnection? SelectBroker(List<BrokerConnection> availableBrokers);
    }
}
