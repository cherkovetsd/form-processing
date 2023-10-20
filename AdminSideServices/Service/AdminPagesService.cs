using Data.Models;
using Data.Requests;
using DatabaseInfo;
using Microsoft.EntityFrameworkCore;
using Razor.Templating.Core;

namespace AdminSideServices.Service
{
    public class AdminPagesService : IAdminPagesService
    {
        private readonly IDbContextFactory<Context> _formContextFactory;

        public AdminPagesService(IDbContextFactory<Context> formContextFactory)
        {
            _formContextFactory = formContextFactory;
        }

        public async Task<string> GetErrorPage(string message)
        {
            var page = await RazorTemplateEngine.RenderAsync("~/Views/Shared/ErrorMessage.cshtml");
            return page;
        }

        public async Task<string> GetEvaluationPage(EvaluationPageRequest request)
        {
            var id = request.Id;
            var timestamp = request.Timestamp;

            await using var context = await _formContextFactory.CreateDbContextAsync();
            var form = context.Forms.Where(f => f.Id == id && f.LastUpdated < timestamp).Include(f => f.Fields).FirstOrDefault();
            if (form != null)
            {
                var result = await context.Forms.Where(f => f.Id == id && f.LastUpdated < timestamp).ExecuteUpdateAsync(
                    setters => setters.SetProperty(f => f.State, f => FormState.UnderEvaluation));

                if (result != 0) 
                {
                    var fieldsForEvaluate = form.Fields.ConvertForUpdatePage(id, timestamp);
                    var page = await RazorTemplateEngine.RenderAsync("~/Views/AdminPages/Evaluate.cshtml", fieldsForEvaluate);

                    return page;
                }                    
            }

            var idExists = await context.Forms.Where(f => f.Id == id).AnyAsync();
            if (idExists)
            {
                return await GetErrorPage("Анкета не найдена");
            }
            return await GetErrorPage("Состояние анкеты изменилось, перезагрузите страницу");
        }

        public async Task<string> GetIndexPage()
        {
            await using var context = await _formContextFactory.CreateDbContextAsync();
            var model = context.Forms.Where(f => f.State == FormState.AwaitingApproval).Include(f => f.Fields);
            var page = await RazorTemplateEngine.RenderAsync("~/Views/AdminPages/Index.cshtml", model);
            return page;
        }
    }
}
