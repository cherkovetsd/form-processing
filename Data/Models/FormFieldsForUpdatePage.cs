namespace Data.Models
{
    public record FormFieldsForUpdatePage(int Id, string? Name, string? Group, string? Topic, string? Company, bool? HasConsultant, DateTime Timestamp)
    {
        public FormFieldsOptional ConvertToOptional()
        {
            return new FormFieldsOptional(Name, Group, Topic, Company, HasConsultant);
        }
    }

}
