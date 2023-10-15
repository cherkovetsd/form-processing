using Utilities.Tasks;

namespace Utilities.Queue
{
    public interface ITaskQueue : IDisposable
    {
        public Task<bool> Push(IQueuedTask task);
    }
}
