using System;
using System.Text.Json.Serialization;

namespace FitnessAgentsWeb.Models
{
    /// <summary>
    /// Archival record of a generated plan, stored alongside its embedding
    /// in the vector store for semantic similarity search.
    /// </summary>
    public class PlanRecord
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("plan_date")]
        public DateTime PlanDate { get; set; }

        [JsonPropertyName("plan_type")]
        public string PlanType { get; set; } = string.Empty;

        [JsonPropertyName("muscle_group")]
        public string MuscleGroup { get; set; } = string.Empty;

        [JsonPropertyName("plan_json")]
        public string PlanJson { get; set; } = string.Empty;

        [JsonPropertyName("plan_summary")]
        public string PlanSummary { get; set; } = string.Empty;

        // Context snapshot at generation time
        [JsonPropertyName("recovery_score")]
        public int RecoveryScore { get; set; }

        [JsonPropertyName("sleep_score")]
        public int SleepScore { get; set; }

        [JsonPropertyName("active_score")]
        public int ActiveScore { get; set; }

        [JsonPropertyName("sleep_total")]
        public string SleepTotal { get; set; } = string.Empty;

        [JsonPropertyName("rhr")]
        public string Rhr { get; set; } = string.Empty;

        [JsonPropertyName("hrv")]
        public string Hrv { get; set; } = string.Empty;

        // Feedback (attached after user rates the plan)
        [JsonPropertyName("feedback")]
        public PlanFeedback? Feedback { get; set; }
    }

    /// <summary>
    /// Result from a vector similarity search on plan records.
    /// </summary>
    public class PlanSearchResult
    {
        public PlanRecord Record { get; set; } = new();
        public float Score { get; set; }
    }
}
