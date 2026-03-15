using FitnessAgentsWeb.Core.Configuration;
using FitnessAgentsWeb.Core.Interfaces;
using FitnessAgentsWeb.Tools;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using System;
using Microsoft.Extensions.Logging;
using System.ClientModel;
using System.Threading.Tasks;

namespace FitnessAgentsWeb.Core.Services
{
    public class NvidiaNimAgentService : BaseAiAgentService, IAiAgentService
    {
        public NvidiaNimAgentService(IAppConfigurationProvider configProvider, ILogger<NvidiaNimAgentService> logger)
            : base(configProvider, logger)
        {
        }

        public async Task<string> GenerateWorkoutAsync(Models.UserHealthContext context)
        {
            IChatClient chatClient = GetChatClient();

            var tools = new HealthDataTools(context);

            AIAgent analystAgent = chatClient.AsAIAgent(
                name: "Physiological_Analyst",
                instructions: $@"You are an elite sports scientist. Your client is {context.FirstName}. 

                    Execute your tools to gather:
                    1. Today's intended Workout Schedule.
                    2. Current Physical Conditions/Injuries.
                    3. Biological Readiness (Sleep, RHR, HRV, Total Burn).
                    4. InBody Baseline (Muscle mass, body fat).
                    5. Current Week's Workout History.

                    ANALYSIS GUIDELINES:
                    - HRV (RMSSD): This is the primary indicator of CNS recovery. If HRV is below 40ms or significantly lower than his baseline, flag 'Low Recovery' and recommend reduced volume.
                    - Total vs Active Burn: Compare today's total calories to active calories. If the gap is small, he has been sedentary; if the total is high, he may need higher caloric intake for the session.
                    - Red Flags: Highlight injuries or high CNS fatigue based on the combination of low HRV and high RHR.

                    Output a structured, clinical summary analyzing if {context.FirstName} is capable of performing his scheduled workout. List exercises he has already done this week to avoid repetition. Do NOT suggest specific exercises.",
                tools: [
                    AIFunctionFactory.Create(tools.GetDailyReadiness),
                    AIFunctionFactory.Create(tools.GetInBodyBaseline),
                    AIFunctionFactory.Create(tools.GetUserConditions),
                    AIFunctionFactory.Create(tools.GetWorkoutSchedule),
                    AIFunctionFactory.Create(tools.GetWeeklyWorkoutHistory)
                ]
            );

            AIAgent coachAgent = chatClient.AsAIAgent(
                name: "Strength_Coach",
                instructions: $@"You are an elite personal trainer specializing in biomechanics and adaptive programming. You are writing an email directly to your client, {context.FirstName}. 
                    You will receive a physiological brief from the Analyst. Your task is to design today's exact workout plan based on the Analyst's report. 

                    FOLLOW THESE STRICT RULES:
                    1. Speak directly to {context.FirstName} in an encouraging, professional tone. 
                    2. Acknowledge his Intended Schedule, but ADAPT if there is localized strain or pain.
                    3. Because {context.FirstName} is a Software Engineer, always include 1-2 specific mobility movements in the warm-up to counteract 'desk posture'.
                    4. Review the Weekly History. DO NOT repeat main working exercises he has already performed this week.
                    5. Review the User Conditions closely. If he states he hated an exercise previously, DO NOT program it. If he reports pain (e.g., piriformis), completely remove exercises that aggravate that area.
                    6. Provide a structured routine: Warm-up, Main Working Sets (with sets/reps), and a Cooldown.
                    7. Output ONLY clean Markdown so it formats nicely in an email.
                
                    Always write in a highly personalized, encouraging tone. End the email by signing off strictly as: 'Stay strong, <br><br>**Apex** <br>*Your AI Biomechanics Specialist*'. Never use generic placeholders."
            );

            var workflow = AgentWorkflowBuilder.BuildSequential([analystAgent, coachAgent]);
            AIAgent workflowAgent = workflow.AsAIAgent(name: "DailyWorkoutEngine");
            AgentSession session = await workflowAgent.CreateSessionAsync();

            _logger.LogInformation("[System] Agents are deliberating...");

            string finalWorkout = "";
            await foreach (var update in workflowAgent.RunStreamingAsync("Generate today's workout plan.", session))
            {
                if (update.Text != null && update.AuthorName == "Strength_Coach")
                    {
                        finalWorkout += update.Text;
                    }
                }
            return finalWorkout;
        }
        public async Task<string> GenerateRecoveryDietJsonAsync(string upcomingWorkoutPlan, Models.UserHealthContext context)
        {
            IChatClient chatClient = GetChatClient();

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
                ""plan_date"": ""string (ISO8601 format)"",
                ""total_calories_target"": ""number (total daily target)"",
                ""meals"": [
                    {{ ""meal_type"": ""string (e.g., Morning, Lunch, Evening, Dinner)"", ""food_name"": ""string"", ""quantity_description"": ""string (e.g., 200g, 1 cup)"", ""calories"": ""number"" }}
                ],
                ""ai_summary"": ""string (Brief expert rationale)""
            }}";

            AIAgent dieticianAgent = chatClient.AsAIAgent(
                name: "Dietician_Planner",
                instructions: "You are an elite sports nutritionist. Your output MUST be a valid JSON object matching the requested schema. Use the schema values as type indicators. DO NOT copy example text. Provide real nutritional data for this specific client."
            );

            AgentSession session = await dieticianAgent.CreateSessionAsync();
            _logger.LogInformation("[Diet AI] Agent is planning recovery diet...");

            try
            {
                string aiResponse = "";
                await foreach (var update in dieticianAgent.RunStreamingAsync(prompt, session))
                {
                    if (update.Text != null)
                    {
                        aiResponse += update.Text;
                    }
                }
                
                _logger.LogInformation($"[Diet AI Raw Response] {aiResponse}");

                return ExtractJson(aiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Diet AI] Error generating diet: {ex.Message}");
                return "{}";
            }
        }
    }
}
