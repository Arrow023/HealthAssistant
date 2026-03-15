using FitnessAgentsWeb.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FitnessAgentsWeb.Core.Services
{
    public class DieticianAgentService
    {
        private readonly Configuration.IAppConfigurationManager _configManager;

        public DieticianAgentService(Configuration.IAppConfigurationManager configManager)
        {
            _configManager = configManager;
        }

        public async Task<string> GenerateRecoveryDietJsonAsync(string upcomingWorkoutPlan, UserHealthContext context)
        {
            string aiKey = _configManager.GetAiKey();
            string aiModel = _configManager.GetAiModel();
            string aiEndpoint = _configManager.GetAiEndpoint();

            string prompt = $@"STRICT RESTRCTION: DO NOT include any reasoning, thought process, or preamble. Return ONLY the JSON object.
            
            You are a leading Clinical Sports Nutritionist.
            Your client, {context.FirstName}, has just received the following training protocol for today:
            
            WORKOUT PLAN:
            {upcomingWorkoutPlan}

            USER BASELINE:
            Weight: {context.InBodyWeight} kg
            Body Fat: {context.InBodyBf}%
            BMR: {context.InBodyBmr} kcal
            Active Calories Burned Today: {context.VitalsCalories}
            Total Calories Burned Today: {context.VitalsTotalCalories}

            Provide a 4-meal macro-optimized diet plan for today.
            
            JSON Schema:
            {{
                ""plan_date"": ""2026-03-15T00:00:00Z"",
                ""total_calories_target"": 2500,
                ""meals"": [
                    {{ ""meal_type"": ""Morning"", ""food_name"": ""Oatmeal"", ""quantity_description"": ""1 cup"", ""calories"": 300 }},
                    {{ ""meal_type"": ""Lunch"", ""food_name"": ""Chicken Breast"", ""quantity_description"": ""200g"", ""calories"": 450 }},
                    {{ ""meal_type"": ""Evening"", ""food_name"": ""Protein Shake"", ""quantity_description"": ""1 scoop"", ""calories"": 150 }},
                    {{ ""meal_type"": ""Dinner"", ""food_name"": ""Salmon"", ""quantity_description"": ""150g"", ""calories"": 400 }}
                ],
                ""ai_summary"": ""A high protein recovery diet focused on muscle synthesis.""
            }}
            ";

            var payload = new
            {
                model = aiModel,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                max_tokens = 8000,
                temperature = 0.1,
                response_format = new { type = "json_object" }
            };

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            
            // Construct full URL if endpoint doesn't end with chat/completions
            string fullUrl = aiEndpoint;
            if (!fullUrl.EndsWith("/chat/completions"))
            {
                fullUrl = fullUrl.TrimEnd('/') + "/chat/completions";
            }

            try
            {
                var response = await client.PostAsync(fullUrl, content);
                string responseJson = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"[Diet AI Raw JSON] {responseJson}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Diet AI HTTP Error] {response.StatusCode}: {responseJson}");
                    return "{}";
                }

                using JsonDocument doc = JsonDocument.Parse(responseJson);
                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    string aiText = "";
                    
                    if (firstChoice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var contentProp))
                    {
                        aiText = contentProp.GetString() ?? "";
                    }

                    Console.WriteLine($"[Diet AI Content] Length: {aiText.Length}");

                    // Strip potential preamble or markdown wrappers
                    int start = aiText.IndexOf('{');
                    int end = aiText.LastIndexOf('}');
                    if (start >= 0 && end > start)
                    {
                        aiText = aiText.Substring(start, end - start + 1);
                    }
                    
                    return aiText.Trim();
                }
                
                return "{}";
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Diet AI Error: {ex.Message}");
                return "{}";
            }
        }
    }
}
