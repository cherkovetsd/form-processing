using Utilities.Tasks;

namespace Utilities.Queue.QueueManager
{
    public class BalancingTaskQueueManager : ITaskQueue
    {
        private readonly List<ICountingTaskQueue> _queues = new();

        private ICountingTaskQueue? _smallestQueue = null;

        public void AddQueue(ICountingTaskQueue queue)
        {
            _queues.Add(queue);
        }

        private void UpdateSmallestQueue()
        {
            _smallestQueue = null;
            uint minCount = uint.MaxValue;

            foreach (var queue in _queues)
            {
                uint count = queue.Count();
                if (count < minCount)
                {
                    _smallestQueue = queue;
                    minCount = count;
                }
            }
        }

        public async Task<bool> Push(IQueuedTask task)
        {
            UpdateSmallestQueue();
            if (_smallestQueue == null)
            {
                return false;
            }
            return await _smallestQueue.Push(task);
        }

        public void Dispose()
        {
            foreach (var queue in _queues)
            {
                queue.Dispose();
            }
        }
    }
}
