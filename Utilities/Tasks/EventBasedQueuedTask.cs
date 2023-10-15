namespace Utilities.Tasks
{
    public abstract class EventBasedQueuedTask : IQueuedTask
    {
        public Task<string> TaskResult { get; }
        private readonly TaskCompletionSource<string> _taskCompletionSource;

        protected EventBasedQueuedTask()
        {
            _taskCompletionSource = new TaskCompletionSource<string>();
            TaskResult = _taskCompletionSource.Task;
        }

        public Task OnCanceled()
        {
            if (!_taskCompletionSource.TrySetCanceled())
            {
                throw new InvalidOperationException("Невозможно отменить EventBasedQueueTask");
            }

            return Task.CompletedTask;
        }

        public void RespondBack(string data)
        {
            _taskCompletionSource.SetResult(data);
        }

        public abstract string SerializeTask();
    }
}
