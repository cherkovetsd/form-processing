using Data.Models;

namespace Data.Requests
{
    public record ChangeStateRequest(int Id, FormState State, DateTime CreateDate);
}
