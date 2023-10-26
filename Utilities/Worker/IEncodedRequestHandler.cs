namespace Utilities.Worker
{
    public interface IEncodedRequestHandler
    {
        public Task<string> CompleteRequest(string encodedRequest);
    }
}