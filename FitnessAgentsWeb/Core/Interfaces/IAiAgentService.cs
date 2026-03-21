using FitnessAgentsWeb.Models;
using System.Threading.Tasks;

namespace FitnessAgentsWeb.Core.Interfaces
{
    public interface IAiAgentService
    {
        Task<string> GenerateWorkoutAsync(UserHealthContext context, string similarPlansContext = "", string recentFeedbackContext = "", string digestContext = "");
        Task<string> GenerateRecoveryDietJsonAsync(string upcomingWorkoutPlan, UserHealthContext context, string similarPlansContext = "", string recentFeedbackContext = "", string digestContext = "");
    }
}
