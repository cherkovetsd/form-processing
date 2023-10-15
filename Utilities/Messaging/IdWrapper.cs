using System.Text.Json;

namespace Utilities.Messaging
{
    public record struct IdWrapper(Guid Id, string Body)
    {
        public static IdWrapper FromJson(string json)
        {
            return JsonSerializer.Deserialize<IdWrapper>(json);
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
