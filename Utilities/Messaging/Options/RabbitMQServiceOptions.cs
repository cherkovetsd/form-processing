namespace Utilities.Messaging.Options
{
    public class RabbitMQServiceOptions
    {
        public class Address
        {
            public string? Hostname { get; set; }
            public int? Port { get; set; }
            public Uri? Uri { get; set; }
        }

        public string? OutcomingQueueName { get; set; }

        public string? IncomingQueueName { get; set; }

        public Address? BrokerAddress { get; set; }

        public TimeSpan? ContinuationTimeout { get; set; }
    }
}
