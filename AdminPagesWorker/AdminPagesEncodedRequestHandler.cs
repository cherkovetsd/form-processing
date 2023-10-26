using AdminSideServices;
using Data.Requests;
using Data.Tasks;
using System.Text.Json;
using AdminSideServices.Service;
using Utilities.Worker;
using static Utilities.Worker.RequestHandlerUtils;

namespace AdminPagesWorker
{
    public class AdminPagesEncodedRequestHandler : IEncodedRequestHandler
    {
        private readonly IAdminPagesService _service;

        public AdminPagesEncodedRequestHandler(IAdminPagesService service)
        {
            _service = service;
        }

        public async Task<string> CompleteRequest(string encodedRequest)
        {
            const string errorMessage = "Некорректное JSON-сообщение";

            try
            {
                var (taskType, message) = RequestMessageWrapper.FromJson(encodedRequest);

                switch (taskType)
                {
                    case RequestType.AdminEvaluatePage:
                        return await HandleJsonRequest<EvaluationPageRequest>(message, errorMessage, _service.GetEvaluationPage);
                    case RequestType.AdminIndexPage:
                        return await _service.GetIndexPage();
                    case RequestType.ErrorPage:
                        return await _service.GetErrorPage(message ?? "");
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
