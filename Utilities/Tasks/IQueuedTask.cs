namespace Utilities.Tasks
{
    public interface IQueuedTask
    {
        Task OnCanceled();

        void RespondBack(string data);

        String SerializeTask();
    }
}
