namespace Utilities.Updating
{
    public interface IUpdateController : IDisposable
    {
        public void Add(IUpdateable queue);

        public void Remove(IUpdateable queue);

        public void Drop();

        public void Start();

        public void Stop();
    }
}