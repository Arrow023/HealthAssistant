using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FitnessAgentsWeb.Models
{
    /// <summary>
    /// Captures user feedback on a generated workout or diet plan.
    /// Used to improve future AI generations via the vector store.
    /// </summary>
    public class PlanFeedback
    {
        [JsonPropertyName("plan_id")]
        public string PlanId { get; set; } = string.Empty;

        [JsonPropertyName("plan_type")]
        public string PlanType { get; set; } = string.Empty;

        [JsonPropertyName("feedback_date")]
        public DateTime FeedbackDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = "just-right";

        [JsonPropertyName("skipped_items")]
        public List<string> SkippedItems { get; set; } = [];

        [JsonPropertyName("note")]
        public string Note { get; set; } = string.Empty;
    }
}
