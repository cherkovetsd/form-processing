namespace Utilities.Queue.Factory.Options
{
    public abstract class RabbitMQQueueListOptions
    {
        public class QueuesAtAddress
        {
            public Address? BrokerAddress { get; set; }

            public QueueOptions[]? QueueList { get; set; } = Array.Empty<QueueOptions>();
        }

        public class Address
        {
            public string? Hostname { get; set; }
            public int? Port { get; set; }
            public Uri? Uri { get; set; }
        }

        public class QueueOptions
        {
            public string? OutcomingQueueName { get; set; }

            public string? IncomingQueueName { get; set; }

            public TimeSpan? ContinuationTimeout { get; set; }
        }

        public QueuesAtAddress[]? QueuesAtAddresses { get; set; } = Array.Empty<QueuesAtAddress>();
    }
}
