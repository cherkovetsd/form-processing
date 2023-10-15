namespace Utilities.Updating
{
    using Microsoft.Extensions.Options;
    using Task = Task;
    public class UpdateController
    {
        private class UpdateableStatus
        {
            public bool IsRunning { get; set; } = true;
        }
        
        private readonly Dictionary<IUpdateable, UpdateableStatus> _statuses = new();

        public void Add(IUpdateable updateable, int delay)
        {
            var status = new UpdateableStatus();
            _statuses[updateable] = status;

            _ = StartUpdating(updateable, delay, status);
        }

        public void Stop(IUpdateable updateable)
        {
            _statuses[updateable].IsRunning = false;
        }
        
        public void Dispose()
        {
            foreach (var status in _statuses)
            {
                status.Value.IsRunning = false;
            }
        }
        
        private static async Task StartUpdating(IUpdateable updateable, int delay, UpdateableStatus status)
        {
            while (status.IsRunning)
            {
                await Task.Delay(delay);
                updateable.Update();
            }
        }
    }
}
