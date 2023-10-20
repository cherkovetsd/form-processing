using Data.Models;
using Data.Requests;
using DatabaseInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Razor.Templating.Core;
using UserSideServices.Options;

namespace UserSideServices.Service
{
    public class UserPagesService : IUserPagesService
    {
        private readonly IDbContextFactory<Context> _formContextFactory;
        private readonly FormState[] _statesAllowedForEdit;

        public UserPagesService(IDbContextFactory<Context> formContextFactory, IOptions<UpdateStateTransitionOptions> options) { 
            _formContextFactory = formContextFactory;
            _statesAllowedForEdit = options.Value.StatesAllowedToEdit;
        }

        public async Task<string> GetIndexPage()
        {
            using (var context = await _formContextFactory.CreateDbContextAsync()) 
            {
                var model = context.Forms.Include(f => f.Fields).ToList();
                var isAllowedForEdit = new Dictionary<string, object>();
                foreach (var record in model)
                {
                    isAllowedForEdit.Add(record.Id.ToString(), _statesAllowedForEdit.Contains(record.State) ? true : false);
                }
                var page = await RazorTemplateEngine.RenderAsync("~/Views/UserPages/Index.cshtml", model, isAllowedForEdit);
                return page;
            }
        }

        public async Task<string> GetCreatePage()
        {
            var page = await RazorTemplateEngine.RenderAsync("~/Views/UserPages/Create.cshtml");
            return page;
        }

        public async Task<string> GetUpdatePage(UpdatePageRequest request)
        {
            var id = request.Id;
            var timestamp = request.Timestamp;

            await using var context = await _formContextFactory.CreateDbContextAsync();
            var formRecord = context.Forms.Where(f => f.Id == id && f.LastUpdated < timestamp).Select(f => f.Fields).FirstOrDefault();
            if (formRecord == null)
            {
                var idExists = await context.Forms.Where(f => f.Id == id).AnyAsync();
                if (idExists)
                {
                    return await GetErrorPage("Анкета не найдена");
                }
                return await GetErrorPage("Состояние анкеты изменилось, перезагрузите страницу");
            }
            var fieldsForUpdate = formRecord.ConvertForUpdatePage(id, timestamp);
            var page = await RazorTemplateEngine.RenderAsync("~/Views/UserPages/Edit.cshtml", fieldsForUpdate);
            return page;
        }

        public async Task<string> GetErrorPage(string message)
        {
            var page = await RazorTemplateEngine.RenderAsync("~/Views/Shared/ErrorMessage.cshtml");
            return page;
        }
    }
}
