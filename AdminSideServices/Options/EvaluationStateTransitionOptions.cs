using Data.Models;

namespace AdminSideServices.Options
{
    public class EvaluationStateTransitionOptions
    {
        public const string Position = "StatesAllowedForEvaluation";

        public FormState[] StatesAllowedForEvaluation { get; set; } = Array.Empty<FormState>();
    }
}
