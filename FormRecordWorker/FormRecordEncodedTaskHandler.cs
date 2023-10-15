using Data.Requests;
using Data.Tasks;
using System.Text.Json;
using UserSideServices.Service;
using Utilities.Worker;
using static Utilities.Worker.RequestHandlerUtils;

namespace FormRecordWorker
{
    public class FormRecordEncodedTaskHandler : IEncodedTaskHandler
    {
        private readonly IFormRecordService _service;

        public FormRecordEncodedTaskHandler(IFormRecordService service)
        {
            _service = service;
        }

        public async Task<string> CompleteTask(string encodedTask)
        {
            string errorMessage = "Некорректное JSON-сообщение";

            try
            {
                var taskWrapper = TaskMessageWrapper.FromJson(encodedTask);
                var message = taskWrapper.Message;

                switch (taskWrapper.Type)
                {
                    case TaskType.FormCreate:
                        return await HandleJsonRequest<AddFormRequest>(message, errorMessage, _service.AddRecord);
                    case TaskType.FormUpdate:
                        return await HandleJsonRequest<UpdateFormRequest>(message, errorMessage, _service.UpdateRecord);
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
