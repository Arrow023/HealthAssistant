namespace FitnessAgentsWeb.Models
{
    public class UserProfile
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string NotificationTime { get; set; } = "08:00"; // HH:mm 24-hour format
        public string Preferences { get; set; } = "No reported pain or injuries."; // Used for AI ConditionsBrief
        public string FoodPreferences { get; set; } = "No specific food preferences."; 
        public bool IsActive { get; set; } = true;
    }
}
