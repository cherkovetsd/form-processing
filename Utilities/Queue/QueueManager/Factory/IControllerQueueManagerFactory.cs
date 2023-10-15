using Utilities.Queue;

namespace Utilities.Queue.QueueManager.Factory
{
    public interface IControllerQueueManagerFactory
    {
        public ITaskQueue GetPageTaskManager();

        public ITaskQueue GetRecordTaskManager();
    }
}
