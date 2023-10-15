namespace Data.Models
{
    public record FormFieldsOptionalDbEntity(string? Name, string? Group, string? Topic, string? Company, bool? HasConsultant) : FormFieldsOptional(Name, Group, Topic, Company, HasConsultant)
    { 
        public int Id { get; set; }

        public FormRecord Record { get; set; } = null!;
    }
}
