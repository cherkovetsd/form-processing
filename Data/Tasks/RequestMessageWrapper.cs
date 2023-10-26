using System.Text.Json;

namespace Data.Tasks
{
    public record struct RequestMessageWrapper(RequestType Type, string? Message = null)
    {
        public static RequestMessageWrapper FromJson(string json)
        {
            return JsonSerializer.Deserialize<RequestMessageWrapper>(json);
        }

        public override string ToString() 
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
