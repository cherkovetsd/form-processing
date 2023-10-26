using Data.Requests;

namespace UserSideServices.Service
{
    public interface IUserPagesService
    {
        public Task<string> GetIndexPage();

        public Task<string> GetAddPage();

        public Task<string> GetUpdatePage(UpdatePageRequest request);

        public Task<string> GetErrorPage(string message);
    }
}
