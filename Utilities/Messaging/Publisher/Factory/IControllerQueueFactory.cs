using Utilities.Queue;

namespace Utilities.Messaging.Publisher.Factory
{
    public interface IControllerQueueFactory
    {
        public ITaskQueue GetPageTaskQueue();

        public ITaskQueue GetRecordTaskQueue();
    }
}
