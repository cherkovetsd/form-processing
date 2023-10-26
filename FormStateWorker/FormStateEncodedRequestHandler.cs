using System.Text.Json;
using AdminSideServices.Service;
using Data.Requests;
using Data.Tasks;
using Utilities.Worker;
using static Utilities.Worker.RequestHandlerUtils;

namespace FormStateWorker
{
    public class FormStateEncodedRequestHandler : IEncodedRequestHandler
    {
        private readonly IFormStateService _service;

        public FormStateEncodedRequestHandler(IFormStateService service)
        {
            _service = service;
        }

        public async Task<string> CompleteRequest(string encodedRequest)
        {
            const string errorMessage = "Некорректное JSON-сообщение";

            try
            {
                var taskWrapper = RequestMessageWrapper.FromJson(encodedRequest);
                var message = taskWrapper.Message;

                switch (taskWrapper.Type)
                {
                    case RequestType.FormStateChange:
                        return await HandleJsonRequest<ChangeStateRequest>(message, errorMessage, _service.ChangeState);
                    case RequestType.EvaluationStateUpdate:
                        return await HandleJsonRequest<EvaluationStateUpdateRequest>(message, errorMessage,
                            _service.UpdateEvaluationState);
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
