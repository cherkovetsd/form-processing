using Data.Requests;

namespace AdminSideServices.Service
{
    public interface IFormStateService
    {
        // Возвращает "success" на успешном обновлении записи, возвращает "error" при возникшей ошибке, и другие сообщения при некорректных данных
        public Task<string> ChangeState(ChangeStateRequest requests);

        // Возвращает "success" на успешном обновлении, возвращает "error" при возникшей ошибке
        public Task<string> UpdateEvaluationState(EvaluationStateUpdateRequest request);
    }
}
