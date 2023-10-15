namespace Utilities.Queue.Factory
{
    public interface IControllerQueueFactory
    {
        public List<ICountingTaskQueue> GetPageTaskQueues();

        public List<ICountingTaskQueue> GetRecordTaskQueues();
    }
}
