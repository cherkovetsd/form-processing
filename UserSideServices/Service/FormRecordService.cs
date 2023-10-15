using System.Linq.Expressions;
using Data.Models;
using Data.Requests;
using DatabaseInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Options;
using UserSideServices.Options;

namespace UserSideServices.Service
{
    public class FormRecordService : IFormRecordService
    {
        private readonly IDbContextFactory<Context> _contextFactory;
        private readonly FormState[] _statesAllowedForEdit;

        public FormRecordService(IDbContextFactory<Context> contextFactory, IOptions<UpdateStateTransitionOptions> options)
        {
            _contextFactory = contextFactory;
            _statesAllowedForEdit = options.Value.StatesAllowedToEdit;
        }

        private static async Task<bool> CheckIfCorrectStudent(Context context, string? name, string? group)
        {
            if (name != null && group != null)
            {
                var student = await context.FindAsync<Student>(name);
                return student != null && student.Group == group;
            }
            else if (name != null)
            {
                var student = context.Students.Where(s => s.Name == name && s.Forms.Any())
                    .Include(s => s.Forms.Where(
                        f => f.Fields.Group == s.Group));
                return await student.AnyAsync();
            }
            else if (group != null)
            {
                var student = context.Students.Where(s => s.Group == group && s.Forms.Any())
                    .Include(s => s.Forms.Where(
                        f => f.Fields.Name == s.Name));
                return await student.AnyAsync();
            }
            else
            {
                return true;
            }
        }

        private static async Task<bool> CheckIfNoApprovedForms(Context context, string? name, int? id = null)
        {
            if (name != null)
            {
                var approvedForms = context.Forms.Where(f => f.Name == name && f.State == FormState.Approved);
                return !await approvedForms.AnyAsync();
            }
            else if (id != null)
            {
                var student = context.Forms.Where(f => f.Id == id && f.Student!.Forms.Any(e => e.State == FormState.Approved));
                return !await student.AnyAsync();
            }
            else
            {
                return true;
            }
        }

        private static async Task<bool> CheckTopicUniqueness(Context context, string? topic)
        {
            if (topic != null)
            {
                var forms = context.Forms.Where(f => f.Fields.Topic == topic);
                return !await forms.AnyAsync();
            }
            return true;
        }

        public async Task<string> AddRecord(AddFormRequest request)
        {
            var fields = request.Fields;
            FormState resultState = FormState.AwaitingApproval;
            string? description = null;

            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                try
                {
                    if (!await CheckIfCorrectStudent(context, fields.Name, fields.Group))
                    {
                        resultState = FormState.ReturnedForRevision;
                        description = "Некорректный студент или группа";
                    }
                    else if (!await CheckIfNoApprovedForms(context, fields.Name))
                    {
                        resultState = FormState.ReturnedForRevision;
                        description = "У переданного студента уже есть утвержденная заявка";
                    }
                    else if (!await CheckTopicUniqueness(context, fields.Topic))
                    {
                        resultState = FormState.ReturnedForRevision;
                        description = "У переданного студента уже есть утвержденная заявка";
                    }

                    var finalForm = new FormRecord { Name = fields.Name, Fields = fields.ConvertToOptionalDbEntity(), State = resultState, Description = description, LastUpdated = DateTime.UtcNow };
                    context.Forms.Add(finalForm);
                    if (resultState == FormState.ReturnedForRevision 
                        || context.FormFields.Any(f => f.Name == fields.Name
                                                       && f.Group == fields.Group
                                                       && f.HasConsultant == fields.HasConsultant
                                                       && f.Topic == fields.Topic
                                                       && f.Company == fields.Company))
                    {
                        return "success";
                    }
                    return await context.SaveChangesAsync() == 0 ? "error" : "success";
                }
                catch (Exception)
                {
                    return "error";
                }
            }
        }

        private static Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>
            AppendSetProperty<TEntity>(Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> left,
                Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> right)
        {
            var replace = new ReplacingExpressionVisitor(right.Parameters, new[] { left.Body });
            var combined = replace.Visit(right.Body);
            return Expression.Lambda<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>(combined, left.Parameters);
        }
        
        private static Expression<Func<SetPropertyCalls<FormFieldsOptionalDbEntity>,
                SetPropertyCalls<FormFieldsOptionalDbEntity>>> CompileFieldsSetter(string? name = null, string? group = null,
                string? topic = null, string? company = null, bool? hasConsultant = null)
        {
            Expression<Func<SetPropertyCalls<FormFieldsOptionalDbEntity>, SetPropertyCalls<FormFieldsOptionalDbEntity>>> fieldsSetter = calls => calls;

            if (name != null)
            {
                fieldsSetter = AppendSetProperty(fieldsSetter, s => s.SetProperty(f => f.Name, name));
            }
            if (group != null)
            {
                fieldsSetter = AppendSetProperty(fieldsSetter, s => s.SetProperty(f => f.Group, group));
            }
            if (topic != null)
            {
                fieldsSetter = AppendSetProperty(fieldsSetter, s => s.SetProperty(f => f.Topic, topic));
            }
            if (company != null)
            {
                fieldsSetter = AppendSetProperty(fieldsSetter, s => s.SetProperty(f => f.Company, company));
            }
            if (hasConsultant != null)
            {
                fieldsSetter = AppendSetProperty(fieldsSetter, s => s.SetProperty(f => f.HasConsultant, hasConsultant));
            }

            return fieldsSetter;
        }
        
        private static async Task<bool> CheckIfChangesApplied(Context context, int id, FormState state, string? name = null, string? group = null,
            string? topic = null, string? company = null, bool? hasConsultant = null)
        {
            var checkChangesQuery = context.FormFields.Where(f => f.Id == id && f.Record.State == state);
            if (name != null)
            {
                checkChangesQuery = checkChangesQuery.Where(f => f.Name == name);
            }
            if (group != null)
            {
                checkChangesQuery = checkChangesQuery.Where(f => f.Group == group);                            
            }
            if (topic != null)
            {
                checkChangesQuery = checkChangesQuery.Where(f => f.Topic == topic);                            
            }
            if (company != null)
            {
                checkChangesQuery = checkChangesQuery.Where(f => f.Company == company);                            
            }
            if (hasConsultant != null)
            {
                checkChangesQuery = checkChangesQuery.Where(f => f.HasConsultant == hasConsultant);                            
            }

            return await checkChangesQuery.AnyAsync();
        }
        
        public async Task<string> UpdateRecord(UpdateFormRequest request)
        {
            var (id, name, group, topic, company, hasConsultant, createDate) = request.Fields;
            var resultState = FormState.AwaitingApproval;
            string? description = null;

            if (name == null && group == null && topic == null && company == null && hasConsultant == null) 
            {
                return "success";
            }

            await using var context = await _contextFactory.CreateDbContextAsync();
            try
            {
                if (!await CheckIfCorrectStudent(context, name, group))
                {
                    resultState = FormState.ReturnedForRevision;
                    description = "Некорректный студент или группа";
                }
                else if (!await CheckIfNoApprovedForms(context, name, id))
                {
                    resultState = FormState.ReturnedForRevision;
                    description = "У переданного студента уже есть утвержденная заявка";
                }
                else if (!await CheckTopicUniqueness(context, topic))
                {
                    resultState = FormState.ReturnedForRevision;
                    description = "У переданного студента уже есть утвержденная заявка";
                }

                Expression<Func<SetPropertyCalls<FormRecord>, SetPropertyCalls<FormRecord>>> recordSetter = calls => calls;

                recordSetter = AppendSetProperty(recordSetter, s => s.SetProperty(f => f.State, resultState));
                recordSetter = AppendSetProperty(recordSetter, s => s.SetProperty(f => f.Description, description));
                recordSetter = AppendSetProperty(recordSetter, s => s.SetProperty(f => f.LastUpdated, DateTime.UtcNow));

                var recordsUpdated = 0;
                var fieldsUpdated = 0;

                await using (var transaction = await context.Database.BeginTransactionAsync())
                {
                    var fieldsSetter = CompileFieldsSetter(name, group, topic, company, hasConsultant);
                    
                    fieldsUpdated = await context.FormFields.Where(f => f.Id == id).Where(f => f.Record.LastUpdated < createDate && _statesAllowedForEdit.Contains(f.Record.State))
                        .ExecuteUpdateAsync(fieldsSetter);
                    recordsUpdated = await context.Forms.Where(f => f.Id == id && f.LastUpdated < createDate && _statesAllowedForEdit.Contains(f.State)).ExecuteUpdateAsync(recordSetter);
                    await transaction.CommitAsync();
                }

                if (recordsUpdated != 0 && fieldsUpdated != 0)
                {
                    return "success";
                }
                
                var isStateAllowed = await context.Forms.Where(f => f.Id == id && _statesAllowedForEdit.Contains(f.State)).AnyAsync();
                if (!isStateAllowed)
                {
                    return "error";
                }
                if (await CheckIfChangesApplied(context, id, resultState, name, group, topic, company, hasConsultant))
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
