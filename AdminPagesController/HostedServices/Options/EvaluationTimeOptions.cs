namespace AdminPagesController.HostedServices.Options;

public class EvaluationTimeOptions
{
    public const string Position = "EvaluationTime";
    
    public TimeSpan? Time { get; set; }
    
    public int? UpdateInterval { get; set; }
}