namespace Data.Models
{
    public record Student(string Name, string Group)
    {
        public ICollection<FormRecord> Forms { get; } = new List<FormRecord>();
    }
}
