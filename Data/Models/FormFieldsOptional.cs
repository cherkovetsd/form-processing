namespace Data.Models
{
    public record FormFieldsOptional(string? Name, string? Group, string? Topic, string? Company, bool? HasConsultant) 
    {
        public FormFieldsForUpdatePage ConvertForUpdatePage(int id, DateTime timestamp)
        {
            return new FormFieldsForUpdatePage(id, Name, Group, Topic, Company, HasConsultant, timestamp);
        }
    }
}
