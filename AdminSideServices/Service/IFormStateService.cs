using Data.Requests;

namespace AdminSideServices.Service
{
    public interface IFormStateService
    {
        // Возвращает "success" на успешном обновлении записи, возвращает "error" при возникшей ошибке, и другие сообщения при некорректных данных
        public Task<string> SetState(ChangeStateRequest requests);
    }
}
