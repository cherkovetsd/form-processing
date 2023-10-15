namespace Data.Models
{
    public record FormFields(string Name, string Group, string Topic, string? Company, bool HasConsultant)
    {
        public FormFieldsOptionalDbEntity ConvertToOptionalDbEntity()
        {
            return new FormFieldsOptionalDbEntity(Name, Group, Topic, Company, HasConsultant);
        }
    }
}
