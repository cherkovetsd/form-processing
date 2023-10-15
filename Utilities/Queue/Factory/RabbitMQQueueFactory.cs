using Utilities.Queue.Factory.Options;
using Utilities.Updating;

namespace Utilities.Queue.Factory
{
    public class RabbitMQQueueFactory
    {
        private readonly IUpdateController _taskQueueController;
        public RabbitMQQueueFactory(IUpdateController taskQueueController)
        {
            _taskQueueController = taskQueueController ?? throw new ArgumentException("No controller given to RabbitMQQueueFactory");
        }

        public List<ICountingTaskQueue> GetQueues(RabbitMQQueueListOptions options)
        {
            if (options.QueuesAtAddresses == null)
            {
                throw new ArgumentException("No options given to RabbitMQQueueFactory");
            }

            var queuesToAddresses = options.QueuesAtAddresses;

            foreach (var option in queuesToAddresses)
            {
                if (option.BrokerAddress == null)
                {
                    throw new ArgumentException("No address given for a queue");
                }

                if (option.QueueList == null || option.QueueList.Length == 0)
                {
                    throw new ArgumentException("No queue given for an address");
                }


                foreach (var queueOption in option.QueueList)
                {
                    if (queueOption.ContinuationTimeout == null || queueOption.IncomingQueueName == null || queueOption.OutcomingQueueName == null)
                    {
                        throw new ArgumentException("Queue parameters missing");
                    }
                }
            }

            List<RabbitMQTaskQueue.ConnectorFactory> factories = new();
            List<ICountingTaskQueue> queues = new();

            try
            {
                foreach (var option in queuesToAddresses)
                {
                    var factory = new RabbitMQTaskQueue.ConnectorFactory(option.BrokerAddress!.Hostname, option.BrokerAddress.Port, option.BrokerAddress.Uri);

                    foreach (var queueOption in option.QueueList!)
                    {
                        var queue = factory.GetQueue(queueOption.OutcomingQueueName!, queueOption.IncomingQueueName!, (TimeSpan)queueOption.ContinuationTimeout!);
                        _taskQueueController.Add(queue);
                        queues.Add(queue);
                    }
                }
            }
            catch (Exception)
            {
                foreach (var factory in factories)
                {
                    factory.Dispose();
                }
                throw;
            }
            return queues;
        }
    }
}
