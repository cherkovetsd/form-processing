namespace Utilities.Messaging
{
    public record struct TaskResult(bool IsCompleted, string? Result = null);
}
