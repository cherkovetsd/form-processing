using System.Text.Json;

namespace Data.Tasks
{
    public record struct TaskMessageWrapper(TaskType Type, string? Message = null)
    {
        public static TaskMessageWrapper FromJson(string json)
        {
            return JsonSerializer.Deserialize<TaskMessageWrapper>(json);
        }

        public override string ToString() 
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
