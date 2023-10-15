using Data.Requests;

namespace UserSideServices.Service
{
    public interface IFormRecordService
    {
        // Возвращает "success" на успешном обновлении записи, возвращает "error" при возникшей ошибке
        public Task<string> AddRecord(AddFormRequest request);

        // Возвращает "success" на успешном обновлении записи, возвращает "error" при возникшей ошибке, и другие сообщения при некорректных данных
        public Task<string> UpdateRecord(UpdateFormRequest request);
    }
}