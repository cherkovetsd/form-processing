namespace Utilities.Updating
{
    using Microsoft.Extensions.Options;
    using Task = Task;
    public class UpdateController : IUpdateController
    {
        private readonly List<IUpdateable> _updateables = new();
        bool _isRunning = false;
        private readonly int _updateInterval;

        public UpdateController(IOptions<UpdateControllerOptions> options)
        {
            _updateInterval = options.Value.UpdateInterval;
        }

        public void Add(IUpdateable updateable)
        {
            _updateables.Add(updateable);
        }

        public void Remove(IUpdateable updateable)
        {
            _updateables.Remove(updateable);
        }
        
        public void Drop()
        {
            _updateables.Clear();
        }

        private void Update()
        {
            foreach (var updateable in _updateables)
            {
                updateable.Update();
            }
        }

        public async void Start()
        {
            _isRunning = true;

            while (_isRunning)
            {
                await Task.Delay(_updateInterval);
                Update();
            }
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
