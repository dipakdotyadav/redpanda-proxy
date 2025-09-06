namespace RedpandaProxy
{
    public class RoundRobinLoadBalancer : ILoadBalancer
    {
        private int _currentIndex = 0;
        private readonly object _lock = new();

        public BrokerConnection? SelectBroker(List<BrokerConnection> availableBrokers)
        {
            if (!availableBrokers.Any()) return null;

            lock (_lock)
            {
                var broker = availableBrokers[_currentIndex % availableBrokers.Count];
                _currentIndex = (_currentIndex + 1) % availableBrokers.Count;
                return broker;
            }
        }
    }
}