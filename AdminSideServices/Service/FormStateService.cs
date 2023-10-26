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
        private readonly IDbContextFactory<Context> _contextFactory;
        private readonly FormState[] _statesAllowedForEvaluate;

        public FormStateService(IDbContextFactory<Context> contextFactory, IOptions<EvaluationStateTransitionOptions> options)
        {
            _contextFactory = contextFactory;
            _statesAllowedForEvaluate = options.Value.StatesAllowedForEvaluation;
        }

        public async Task<string> ChangeState(ChangeStateRequest request)
        {
            var id = request.Id;
            var createDate = request.CreateDate;
            var state = request.State;
            
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                
                var result = await context.Forms
                    .Where(f => f.Id == id && f.LastUpdated < createDate &&
                                _statesAllowedForEvaluate.Contains(f.State))
                    .ExecuteUpdateAsync(s =>
                        s.SetProperty(f => f.State, f => state).SetProperty(f => f.LastUpdated, DateTime.UtcNow));
                if (result != 0)
                {
                    return "success";
                }
                if (await context.Forms.AnyAsync(f => f.Id == id && f.State == state))
                {
                    return "success";
                }
                var isStateAllowed = await context.Forms.Where(f => f.Id == id && f.LastUpdated < createDate && !_statesAllowedForEvaluate.Contains(f.State)).AnyAsync();
                return !isStateAllowed ? "error" : "Состояние анкеты изменилось, перезагрузите страницу";
            } 
            catch (Exception) 
            {
                return "error";
            }
        }

        public async Task<string> UpdateEvaluationState(EvaluationStateUpdateRequest request)
        {
            var evaluationTime = request.EvaluationTime;
            
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                await context.Forms.Where(f =>
                    f.State == FormState.UnderEvaluation && f.LastUpdated < (DateTime.UtcNow - evaluationTime)).ExecuteUpdateAsync(
                    s =>
                        s.SetProperty(f => f.State, f => FormState.AwaitingApproval)
                            .SetProperty(f => f.LastUpdated, DateTime.UtcNow));
                return "success";
            }
            catch (Exception)
            {
                return "error";
            }
            
        }
    }
}
