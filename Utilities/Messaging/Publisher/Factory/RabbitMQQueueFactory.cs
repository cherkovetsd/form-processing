using Microsoft.Extensions.Options;
using Utilities.Messaging.Publisher.Factory.Options;
using Utilities.Queue;
using Utilities.Updating;

namespace Utilities.Messaging.Publisher.Factory
{
    public class RabbitMQQueueFactory : IControllerQueueFactory
    {
        private readonly ITaskQueue _pageTaskQueue;
        private readonly ITaskQueue _recordTaskQueue;

        public RabbitMQQueueFactory(IOptions<PageTaskQueueOptions> pageQueueOptions, IOptions<RecordTaskQueueOptions> recordQueueOptions, UpdateController updateController)
        {
            _pageTaskQueue = new RabbitMQTaskQueueWithId(pageQueueOptions.Value, updateController);
            _recordTaskQueue = new RabbitMQTaskQueueWithId(recordQueueOptions.Value, updateController);
        }

        public ITaskQueue GetPageTaskQueue()
        {
            return _pageTaskQueue;
        }

        public ITaskQueue GetRecordTaskQueue()
        {
            return _recordTaskQueue;
        }
    }
}
