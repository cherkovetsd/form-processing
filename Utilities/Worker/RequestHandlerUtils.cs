using System.Text.Json;

namespace Utilities.Worker
{
    public static class RequestHandlerUtils
    {
        public static async Task<string> HandleJsonRequest<T>(string? json, string errorMessage, Func<T, Task<string>> handlerFunc)
        {
            if (json == null)
            {
                return errorMessage;
            }
            var request = JsonSerializer.Deserialize<T>(json);
            return request == null ? errorMessage : await handlerFunc(request);
        }
    }
}
