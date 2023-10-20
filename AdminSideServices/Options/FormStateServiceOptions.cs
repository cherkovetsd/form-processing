using Data.Models;

namespace AdminSideServices.Options
{
    public class FormStateServiceOptions
    {
        public const string Position = "StatesAllowedForEvaluation";

        public FormState[] StatesAllowedForEvaluation { get; set; } = Array.Empty<FormState>();
    }
}
