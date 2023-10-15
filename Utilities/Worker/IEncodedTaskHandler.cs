namespace Utilities.Worker
{
    public interface IEncodedTaskHandler
    {
        public Task<string> CompleteTask(string encodedTask);
    }
}