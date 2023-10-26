using Data.Requests;
using Data.Tasks;
using System.Text.Json;
using UserSideServices.Service;
using Utilities.Worker;
using static Utilities.Worker.RequestHandlerUtils;

namespace UserPagesWorker
{
    public class UserPagesEncodedRequestHandler : IEncodedRequestHandler
    {
        private readonly IUserPagesService _service;

        public UserPagesEncodedRequestHandler(IUserPagesService service)
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
                    case RequestType.UserAddPage:
                        return await _service.GetAddPage();
                    case RequestType.UserUpdatePage:
                        return await HandleJsonRequest<UpdatePageRequest>(message, errorMessage, _service.GetUpdatePage);
                    case RequestType.UserIndexPage:
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
