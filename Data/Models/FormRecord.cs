namespace Data.Models
{
    public class FormRecord {
        public required FormFieldsOptionalDbEntity Fields { get; set; } // Using FormFieldsOptional for ease of updating entities
        public FormState State { get; set; }
        public string? Description { get; set; }
        public int Id { get; set; }
        public DateTime LastUpdated { get; set; }
        public string? Name { get; set; }

        public Student? Student { get; set; } = null!;
    };
}
