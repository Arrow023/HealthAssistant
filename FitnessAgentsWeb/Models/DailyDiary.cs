using System;
using System.Collections.Generic;

namespace FitnessAgentsWeb.Models
{
    public class DailyDiary
    {
        public string Date { get; set; } = string.Empty; // yyyy-MM-dd
        public List<DiaryMeal> ActualMeals { get; set; } = new();
        public List<DiaryWorkoutLog> WorkoutLog { get; set; } = new();
        public List<DiaryPainLog> PainLog { get; set; } = new();
        public int MoodEnergy { get; set; } // 1-5
        public double WaterIntakeLitres { get; set; }
        public string SleepNotes { get; set; } = string.Empty;
        public string GeneralNotes { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class DiaryMeal
    {
        public string MealTime { get; set; } = string.Empty; // Morning, Lunch, Evening, Dinner, Snack
        public string FoodName { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public bool WasFromPlan { get; set; } // Did they eat what was recommended?
        public string Substitution { get; set; } = string.Empty; // If they swapped, what was the original?
    }

    public class DiaryWorkoutLog
    {
        public string Exercise { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public string Feeling { get; set; } = string.Empty; // easy, moderate, hard, skipped
        public string Notes { get; set; } = string.Empty;
    }

    public class DiaryPainLog
    {
        public string BodyArea { get; set; } = string.Empty;
        public int Severity { get; set; } // 1-5
        public string Description { get; set; } = string.Empty;
    }
}
