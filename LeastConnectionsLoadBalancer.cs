namespace RedpandaProxy
{
    public class LeastConnectionsLoadBalancer : ILoadBalancer
    {
        public BrokerConnection? SelectBroker(List<BrokerConnection> availableBrokers)
        {
            if (!availableBrokers.Any()) return null;
            return availableBrokers.OrderBy(b => b.ActiveConnections).First();
        }
    }
}