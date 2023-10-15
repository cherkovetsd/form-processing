using AdminSideServices;
using Data.Requests;
using Data.Tasks;
using System.Text.Json;
using AdminSideServices.Service;
using Utilities.Worker;
using static Utilities.Worker.RequestHandlerUtils;

namespace AdminPagesWorker
{
    public class AdminPagesEncodedTaskHandler : IEncodedTaskHandler
    {
        private readonly IAdminPagesService _service;

        public AdminPagesEncodedTaskHandler(IAdminPagesService service)
        {
            _service = service;
        }

        public async Task<string> CompleteTask(string encodedTask)
        {
            const string errorMessage = "Некорректное JSON-сообщение";

            try
            {
                var (taskType, message) = TaskMessageWrapper.FromJson(encodedTask);

                switch (taskType)
                {
                    case TaskType.AdminEvaluatePage:
                        return await HandleJsonRequest<EvaluationPageRequest>(message, errorMessage, _service.GetEvaluationPage);
                    case TaskType.AdminIndexPage:
                        return await _service.GetIndexPage();
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
