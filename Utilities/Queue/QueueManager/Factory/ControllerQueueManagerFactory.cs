using Utilities.Queue.Factory;

namespace Utilities.Queue.QueueManager.Factory
{
    public class ControllerQueueManagerFactory : IControllerQueueManagerFactory
    {
        private readonly IControllerQueueFactory _queueFactory;
        private readonly ITaskQueue _pageTaskManager;
        private readonly ITaskQueue _recordTaskManager;

        public ControllerQueueManagerFactory(IControllerQueueFactory queueFactory)
        {
            _queueFactory = queueFactory;

            var pageTaskQueues = _queueFactory.GetPageTaskQueues();
            var recordTaskQueues = _queueFactory.GetRecordTaskQueues();

            _pageTaskManager = GetManager(pageTaskQueues);
            _recordTaskManager = GetManager(recordTaskQueues);
        }

        private ITaskQueue GetManager(List<ICountingTaskQueue> queues)
        {
            try
            {
                var queueManager = new BalancingTaskQueueManager();

                foreach (var queue in queues)
                {
                    queueManager.AddQueue(queue);
                }

                return new RePushingTaskQueueManager(queueManager);
            }
            catch (Exception)
            {
                foreach (var queue in queues)
                {
                    queue.Dispose();
                }
                throw;
            }
        }

        public ITaskQueue GetPageTaskManager()
        {
            return _pageTaskManager;
        }

        public ITaskQueue GetRecordTaskManager()
        {
            return _recordTaskManager;
        }
    }
}
