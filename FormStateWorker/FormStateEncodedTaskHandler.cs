using System.Text.Json;
using AdminSideServices.Service;
using Data.Requests;
using Data.Tasks;
using Utilities.Worker;
using static Utilities.Worker.RequestHandlerUtils;

namespace FormStateWorker
{
    public class FormStateEncodedTaskHandler : IEncodedTaskHandler
    {
        private readonly IFormStateService _service;

        public FormStateEncodedTaskHandler(IFormStateService service)
        {
            _service = service;
        }

        public async Task<string> CompleteTask(string encodedTask)
        {
            const string errorMessage = "Некорректное JSON-сообщение";

            try
            {
                var taskWrapper = TaskMessageWrapper.FromJson(encodedTask);
                var message = taskWrapper.Message;

                switch (taskWrapper.Type)
                {
                    case TaskType.FormStateChange:
                        return await HandleJsonRequest<ChangeStateRequest>(message, errorMessage, _service.SetState);
                    default:
                        return errorMessage;
                }
            }
            catch (JsonException)
            {
                return errorMessage;
            }
        }
    }
}
