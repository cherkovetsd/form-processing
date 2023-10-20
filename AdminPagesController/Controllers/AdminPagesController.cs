using AdminSideServices;
using Data.Models;
using Data.Requests;
using Data.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminSideServices.Service;
using Utilities.Messaging.Publisher.Factory;
using Utilities.Queue;
using Utilities.Tasks;
using static Utilities.Controller.ControllerTools;

namespace AdminPagesController.Controllers
{
    public class AdminPagesController : Controller
    {
        private const string ContentType = "text/html";
        private readonly ITaskQueue _pageTaskQueueManager;
        private readonly ITaskQueue _recordTaskQueueManager;
        private readonly IAdminPagesService _pagesService;
        private readonly IFormStateService _formService;

        public AdminPagesController(IControllerQueueFactory queueFactory, IAdminPagesService pagesService,
            IFormStateService formService)
        {
            _pageTaskQueueManager = queueFactory.GetPageTaskQueue();
            _recordTaskQueueManager = queueFactory.GetRecordTaskQueue();
            _pagesService = pagesService;
            _formService = formService;
        }

        private class EvaluateFormTask : EventBasedQueuedTask
        {
            private static readonly JsonSerializerOptions Options = new JsonSerializerOptions()
                { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

            private ChangeStateRequest Request { get; }

            public EvaluateFormTask(ChangeStateRequest request)
            {
                Request = request;
            }

            public override string SerializeTask()
            {
                return new TaskMessageWrapper(TaskType.FormStateChange, JsonSerializer.Serialize(Request, Options))
                    .ToString();
            }
        }

        private class GetIndexPageTask : EventBasedQueuedTask
        {
            public override string SerializeTask()
            {
                return new TaskMessageWrapper(TaskType.AdminIndexPage).ToString();
            }
        }

        private class GetEvaluationPageTask : EventBasedQueuedTask
        {
            private readonly EvaluationPageRequest _request;

            public GetEvaluationPageTask(EvaluationPageRequest request)
            {
                _request = request;
            }

            public override string SerializeTask()
            {
                return new TaskMessageWrapper(TaskType.AdminEvaluatePage, JsonSerializer.Serialize(_request))
                    .ToString();
            }
        }

        private class GetErrorPageTask : EventBasedQueuedTask
        {
            private readonly string _message;

            public GetErrorPageTask(string message)
            {
                _message = message;
            }

            public override string SerializeTask()
            {
                return new TaskMessageWrapper(TaskType.ErrorPage, _message).ToString();
            }
        }

        private async Task<IActionResult> GetPage(EventBasedQueuedTask task, ITaskQueue queueManager,
            Func<Task<string>> fallback)
        {
            var body = await CompletePageAction(task, queueManager, fallback,
                () => _pagesService.GetErrorPage("error"));
            return base.Content(body, ContentType);
        }

        public async Task<IActionResult> Index()
        {
            return await GetPage(new GetIndexPageTask(), _pageTaskQueueManager, _pagesService.GetIndexPage);
        }

        public async Task<IActionResult> Evaluate(int? id, [FromBody] DateTime? createDate = null)
        {
            if (id == null)
            {
                return BadRequest();
            }

            if (createDate == null)
            {
                createDate = DateTime.UtcNow;
            }

            var request = new EvaluationPageRequest((int)id, (DateTime)createDate);

            return await GetPage(new GetEvaluationPageTask(request), _pageTaskQueueManager,
                () => _pagesService.GetEvaluationPage(request));
        }

        public async Task<IActionResult> Error(string message)
        {
            return await GetPage(new GetErrorPageTask(message), _pageTaskQueueManager,
                () => _pagesService.GetErrorPage(message));
        }

        public class EvaluateParams
        {
            public required FormState State { get; set; }
            public DateTime Timestamp { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Evaluate(int id, [Bind("State,Timestamp")] EvaluateParams evaluateParams)
        {
            var state = evaluateParams.State;
            var timestamp = evaluateParams.Timestamp;
            if (state != FormState.Approved && state != FormState.Rejected)
            {
                return BadRequest();
            }

            var request = new ChangeStateRequest(id, state, timestamp);
            var task = new EvaluateFormTask(request);
            var result = await CompleteFormAction(task, _recordTaskQueueManager, () => _formService.SetState(request),
                Index, Error);
            return Redirect("~/AdminPages/Index");
        }
    }
}