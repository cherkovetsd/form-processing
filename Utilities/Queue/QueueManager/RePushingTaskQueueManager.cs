using Utilities.Tasks;

namespace Utilities.Queue.QueueManager
{
    public class RePushingTaskQueueManager : ITaskQueue
    {
        private ITaskQueue _innerQueue;

        public RePushingTaskQueueManager(ITaskQueue innerManager)
        {
            _innerQueue = innerManager;
        }

        public async Task<bool> Push(IQueuedTask task)
        {
            return await _innerQueue.Push(new ManagedQueuedTask(this, task));
        }

        protected record ManagedQueuedTask(RePushingTaskQueueManager QueueManager, IQueuedTask InnerTask) : IQueuedTask
        {
            public void RespondBack(string data)
            {
                InnerTask.RespondBack(data);
            }

            public string SerializeTask()
            {
                return InnerTask.SerializeTask();
            }

            protected async Task<bool> AttemptPushing()
            {
                return await QueueManager.Push(InnerTask);
            }

            public async Task OnCanceled()
            {
                if (await AttemptPushing())
                {
                    return;
                }
                await InnerTask.OnCanceled();
            }
        }

        protected ManagedQueuedTask DecorateTask(IQueuedTask task)
        {
            return new ManagedQueuedTask(this, task);
        }

        public void Dispose()
        {
            _innerQueue.Dispose();
        }
    }
}
