using Data.Models;

namespace Data.Requests
{
    public record AddFormRequest(FormFields Fields, DateTime CreateDate);
}
