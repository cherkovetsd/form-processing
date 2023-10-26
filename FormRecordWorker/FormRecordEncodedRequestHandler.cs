using Data.Requests;
using Data.Tasks;
using System.Text.Json;
using UserSideServices.Service;
using Utilities.Worker;
using static Utilities.Worker.RequestHandlerUtils;

namespace FormRecordWorker
{
    public class FormRecordEncodedRequestHandler : IEncodedRequestHandler
    {
        private readonly IFormRecordService _service;

        public FormRecordEncodedRequestHandler(IFormRecordService service)
        {
            _service = service;
        }

        public async Task<string> CompleteRequest(string encodedRequest)
        {
            string errorMessage = "Некорректное JSON-сообщение";

            try
            {
                var taskWrapper = RequestMessageWrapper.FromJson(encodedRequest);
                var message = taskWrapper.Message;

                switch (taskWrapper.Type)
                {
                    case RequestType.FormAdd:
                        return await HandleJsonRequest<AddFormRequest>(message, errorMessage, _service.AddRecord);
                    case RequestType.FormUpdate:
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
