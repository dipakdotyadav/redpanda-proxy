namespace RedpandaProxy
{
    public class RandomLoadBalancer : ILoadBalancer
    {
        private readonly Random _random = new();

        public BrokerConnection? SelectBroker(List<BrokerConnection> availableBrokers)
        {
            if (!availableBrokers.Any()) return null;
            return availableBrokers[_random.Next(availableBrokers.Count)];
        }
    }
}