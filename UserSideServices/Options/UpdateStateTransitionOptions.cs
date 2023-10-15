using Data.Models;

namespace UserSideServices.Options
{
    public class UpdateStateTransitionOptions
    {
        public const string Position = "StatesAllowedToEdit";

        public FormState[] StatesAllowedToEdit { get; set; } = Array.Empty<FormState>();
    }
}
