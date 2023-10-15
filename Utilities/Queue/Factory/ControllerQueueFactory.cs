using Microsoft.Extensions.Options;
using Utilities.Queue.Factory.Options;

namespace Utilities.Queue.Factory
{
    public class ControllerQueueFactory : IControllerQueueFactory
    {
        private readonly RabbitMQQueueFactory _queueFactory;
        private readonly RabbitMQQueueListOptions _pageTaskOptions;
        private readonly RabbitMQQueueListOptions _recordTaskOptions;

        public ControllerQueueFactory(RabbitMQQueueFactory queueFactory, IOptions<PageTaskQueueListOptions> pageTaskOptions, IOptions<RecordTaskQueueListOptions> recordTaskOptions)
        {
            _queueFactory = queueFactory;
            _pageTaskOptions = pageTaskOptions.Value;
            _recordTaskOptions = recordTaskOptions.Value;
        }

        public List<ICountingTaskQueue> GetPageTaskQueues()
        {
            return _queueFactory.GetQueues(_pageTaskOptions);
        }

        public List<ICountingTaskQueue> GetRecordTaskQueues()
        {
            return _queueFactory.GetQueues(_recordTaskOptions);
        }
    }
}
