using Data.Requests;

namespace AdminSideServices.Service
{
    public interface IAdminPagesService
    {
        public Task<string> GetIndexPage();

        public Task<string> GetEvaluationPage(EvaluationPageRequest request);

        public Task<string> GetErrorPage(string message);
    }
}
