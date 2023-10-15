using Data.Requests;
using Data.Tasks;
using System.Text.Json;
using UserSideServices.Service;
using Utilities.Worker;
using static Utilities.Worker.RequestHandlerUtils;

namespace UserPagesWorker
{
    public class UserPagesEncodedTaskHandler : IEncodedTaskHandler
    {
        private readonly IUserPagesService _service;

        public UserPagesEncodedTaskHandler(IUserPagesService service)
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
                    case TaskType.UserCreatePage:
                        return await _service.GetCreatePage();
                    case TaskType.UserUpdatePage:
                        return await HandleJsonRequest<UpdatePageRequest>(message, errorMessage, _service.GetUpdatePage);
                    case TaskType.UserIndexPage:
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
