using AdminSideServices.Options;
using Data.Models;
using Data.Requests;
using DatabaseInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AdminSideServices.Service
{
    public class FormStateService : IFormStateService
    {
        private readonly IDbContextFactory<Context> _formContextFactory;
        private readonly FormState[] _statesAllowedForEvaluate;

        public FormStateService(IDbContextFactory<Context> formContextFactory, IOptions<EvaluationStateTransitionOptions> options)
        {
            _formContextFactory = formContextFactory;
            _statesAllowedForEvaluate = options.Value.StatesAllowedForEvaluation;
        }

        public async Task<string> SetState(ChangeStateRequest request)
        {
            var id = request.Id;
            var createDate = request.CreateDate;
            var state = request.State;

            await using var context = await _formContextFactory.CreateDbContextAsync();
            try
            {
                var result = await context.Forms
                    .Where(f => f.Id == id && f.LastUpdated < createDate &&
                                _statesAllowedForEvaluate.Contains(f.State))
                    .ExecuteUpdateAsync(s =>
                        s.SetProperty(f => f.State, f => state).SetProperty(f => f.LastUpdated, DateTime.UtcNow));
                if (result != 0)
                {
                    return "success";
                }
                var isStateAllowed = await context.Forms.Where(f => f.Id == id && f.LastUpdated < createDate && _statesAllowedForEvaluate.Contains(f.State)).AnyAsync();
                if (!isStateAllowed)
                {
                    return "error";
                }
                if (await context.Forms.AnyAsync(f => f.Id == id && f.State == state))
                {
                    return "success";
                }
                return "Состояние анкеты изменилось, перезагрузите страницу";
            } 
            catch (Exception) 
            {
                return "error";
            }
        }
    }
}
