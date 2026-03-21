using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FitnessAgentsWeb.Models
{
    /// <summary>
    /// Aggregated weekly summary of diary entries, stored in Firebase
    /// and embedded in Qdrant for long-term behavioral memory.
    /// </summary>
    public class WeeklyDigest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("week_start")]
        public string WeekStart { get; set; } = string.Empty; // yyyy-MM-dd (Monday)

        [JsonPropertyName("week_end")]
        public string WeekEnd { get; set; } = string.Empty; // yyyy-MM-dd (Sunday)

        [JsonPropertyName("diary_days")]
        public int DiaryDays { get; set; } // How many days had diary entries

        [JsonPropertyName("digest_text")]
        public string DigestText { get; set; } = string.Empty; // Full summary for embedding

        // Aggregated behavioral stats
        [JsonPropertyName("avg_mood")]
        public double AvgMood { get; set; }

        [JsonPropertyName("avg_water")]
        public double AvgWater { get; set; }

        [JsonPropertyName("workout_completion_pct")]
        public double WorkoutCompletionPct { get; set; }

        [JsonPropertyName("total_exercises_done")]
        public int TotalExercisesDone { get; set; }

        [JsonPropertyName("total_exercises_skipped")]
        public int TotalExercisesSkipped { get; set; }

        [JsonPropertyName("recurring_pains")]
        public List<string> RecurringPains { get; set; } = [];

        [JsonPropertyName("consistent_meals")]
        public List<string> ConsistentMeals { get; set; } = [];

        [JsonPropertyName("frequently_skipped")]
        public List<string> FrequentlySkipped { get; set; } = [];

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
