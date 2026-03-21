namespace FitnessAgentsWeb.Models.ViewModels
{
    /// <summary>
    /// View model for the plan feedback form shown on Workout/Diet detail pages.
    /// </summary>
    public class PlanFeedbackViewModel
    {
        public required string UserId { get; set; }
        public required string DayOfWeek { get; set; }
        public required string PlanType { get; set; }
        public int Rating { get; set; }
        public string Difficulty { get; set; } = "just-right";
        public string? SkippedItems { get; set; }
        public string? Note { get; set; }

        // Existing feedback (for display)
        public PlanFeedback? ExistingFeedback { get; set; }
    }
}
