namespace RedpandaProxy
{
    
    public class WeightedLoadBalancer : ILoadBalancer
    {
        private readonly Random _random = new();

        public BrokerConnection? SelectBroker(List<BrokerConnection> availableBrokers)
        {
            if (!availableBrokers.Any()) return null;

            var totalWeight = availableBrokers.Sum(b => b.Config.Weight);
            var randomValue = _random.Next(totalWeight);
            var currentWeight = 0;

            foreach (var broker in availableBrokers)
            {
                currentWeight += broker.Config.Weight;
                if (randomValue < currentWeight)
                    return broker;
            }

            return availableBrokers.First();
        }
    }
}