using System.Text.Json;
using System.Text.Json.Serialization;
using Data.Models;
using Data.Requests;
using Data.Tasks;
using Microsoft.AspNetCore.Mvc;
using UserSideServices.Service;
using Utilities.Messaging.Publisher.Factory;
using Utilities.Queue;
using Utilities.Tasks;
using static Utilities.Controller.ControllerTools;

namespace UserPagesController.Controllers
{
    public class UserPagesController : Controller
    {
        private const string ContentType = "text/html";
        private readonly ITaskQueue _pageTaskQueue;
        private readonly ITaskQueue _recordTaskQueue;
        private readonly IUserPagesService _pagesService;
        private readonly IFormRecordService _formService;

        public UserPagesController(IControllerQueueFactory queueFactory, IUserPagesService pagesService, IFormRecordService formService)
        {
            _pageTaskQueue = queueFactory.GetPageTaskQueue();
            _recordTaskQueue = queueFactory.GetRecordTaskQueue();    
            _pagesService = pagesService;
            _formService = formService;
        }

        private class AddFormTask : EventBasedQueuedTask
        {
            private AddFormRequest Request { get; }

            public AddFormTask(AddFormRequest request) 
            {
                Request = request;
            }
            public override string SerializeTask()
            {
                return new RequestMessageWrapper(RequestType.FormAdd, JsonSerializer.Serialize(Request)).ToString();
            }
        }

        private class UpdateFormTask : EventBasedQueuedTask
        {
            private static readonly JsonSerializerOptions options = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

            private UpdateFormRequest Request { get; }

            public UpdateFormTask(UpdateFormRequest request)
            {
                Request = request;
            }
            public override string SerializeTask()
            {
                return new RequestMessageWrapper(RequestType.FormUpdate, JsonSerializer.Serialize(Request, options)).ToString();
            }
        }

        private class GetIndexPageTask : EventBasedQueuedTask
        {
            public override string SerializeTask()
            {
                return new RequestMessageWrapper(RequestType.UserIndexPage).ToString();
            }
        }

        private class GetCreatePageTask : EventBasedQueuedTask
        {
            public override string SerializeTask()
            {
                return new RequestMessageWrapper(RequestType.UserAddPage).ToString();
            }
        }

        private class GetUpdatePageTask : EventBasedQueuedTask
        {
            private readonly UpdatePageRequest _request;

            public GetUpdatePageTask(UpdatePageRequest request)
            {
                _request = request;
            }

            public override string SerializeTask()
            {
                return new RequestMessageWrapper(RequestType.UserUpdatePage, JsonSerializer.Serialize(_request)).ToString();
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
                return new RequestMessageWrapper(RequestType.ErrorPage, _message).ToString();
            }
        }

        private async Task<IActionResult> GetPage(EventBasedQueuedTask task, ITaskQueue queueManager, Func<Task<string>> fallback)
        {
            var body = await CompletePageAction(task, queueManager, fallback, () => _pagesService.GetErrorPage("error"));
            return base.Content(body, ContentType);
        }

        public async Task<IActionResult> Index()
        {
            return await GetPage(new GetIndexPageTask(), _pageTaskQueue, _pagesService.GetIndexPage);
        }

        public async Task<IActionResult> Create()
        {
            return await GetPage(new GetCreatePageTask(), _pageTaskQueue, _pagesService.GetAddPage);
        }

        public async Task<IActionResult> Edit(int? id, [FromBody] DateTime? createDate=null)
        {
            if (id == null)
            {
                return BadRequest();
            }
            if (createDate == null)
            {
                createDate = DateTime.UtcNow;
            }
            var request = new UpdatePageRequest((int)id, (DateTime)createDate);

            return await GetPage(new GetUpdatePageTask(request), _pageTaskQueue, () => _pagesService.GetUpdatePage(request));
        }

        public async Task<IActionResult> Error(string message)
        {
            return await GetPage(new GetErrorPageTask(message), _pageTaskQueue, () => _pagesService.GetErrorPage(message));
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Name,Group,Topic,Company,HasConsultant")] FormFields fields)
        {
            if (ModelState.IsValid)
            {
                var request = new AddFormRequest(fields, DateTime.Now);
                return await CompleteFormAction(new AddFormTask(request), _recordTaskQueue, ()=> _formService.AddRecord(request), Index, Error);
            }
            return Redirect("~/UserPages/Create");
        }

        [HttpPost]
        public async Task<IActionResult> Edit([Bind("Id, Name,Group,Topic,Company,HasConsultant,Timestamp")] FormFieldsForUpdatePage fields)
        {
            if (ModelState.IsValid)
            {
                var request = new UpdateFormRequest(fields);
                var task = new UpdateFormTask(request);
                var result = await CompleteFormAction(task, _recordTaskQueue, () => _formService.UpdateRecord(request), Index, Error);
                return Redirect("~/UserPages/Index");
            }
            return Redirect("~/UserPages/Edit/" + fields.Id);
        }
    }
}
