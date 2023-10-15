using Microsoft.AspNetCore.Mvc;
using Utilities.Queue;
using Utilities.Tasks;

namespace Utilities.Controller
{
    public static class ControllerTools
    {
        private static async Task<string> CompleteTask(EventBasedQueuedTask task, ITaskQueue queue)
        {
            var taskAccepted = await queue.Push(task);

            if (taskAccepted)
            {
                await task.TaskResult;
                if (task.TaskResult.IsCompleted)
                {
                    return task.TaskResult.Result;
                }
            }
            return "error";
        }

        public static async Task<string> CompletePageAction(EventBasedQueuedTask task, ITaskQueue queue,
            Func<Task<string>> fallback, Func<Task<String>> error)
        {
            var result = await CompleteTask(task, queue);
            if (result == "error")
            {
                result = await fallback();
            }
            if (result == "error")
            {
                return await error();
            }
            return result;
        }

        public static async Task<IActionResult> CompleteFormAction(EventBasedQueuedTask task, ITaskQueue queue,
            Func<Task<string>> fallback, Func<Task<IActionResult>> index, Func<string, Task<IActionResult>> error)
        {
            var result = await CompleteTask(task, queue);
            if (result == "error")
            {
                result = await fallback();
            }
            if (result == "success")
            {
                return await index();
            }
            return await error(result);
        }

        public static async Task CompleteFireAndForgetAction(EventBasedQueuedTask task, ITaskQueue queue,
            Func<Task> fallback)
        {
            var result = await CompleteTask(task, queue);
            if (result == "error")
            {
                await fallback();
            }
        }
    }
}
