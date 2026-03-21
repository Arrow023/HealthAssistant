using FitnessAgentsWeb.Core.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FitnessAgentsWeb.Controllers
{
    public class SetupController : Controller
    {
        private readonly IAppConfigurationManager _configManager;

        public SetupController(IAppConfigurationManager configManager)
        {
            _configManager = configManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (await _configManager.IsAppConfiguredAsync())
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(
            string adminEmail, 
            string adminPassword, 
            string aiModel, 
            string aiEndpoint, 
            string aiKey, 
            string ocrModel,
            string ocrEndpoint,
            string ocrKey,
            string smtpHost, 
            string smtpPort, 
            string fromEmail,
            string smtpPassword,
            string timezone,
            string qdrantEndpoint,
            string qdrantApiKey,
            string embeddingModel,
            string embeddingEndpoint,
            string embeddingApiKey,
            string embeddingDimension)
        {
            await _configManager.SaveSetupSettingsAsync(
                adminEmail, adminPassword, aiModel, aiEndpoint, aiKey, 
                ocrModel, ocrEndpoint, ocrKey,
                smtpHost, smtpPort, fromEmail, smtpPassword, timezone, "",
                qdrantEndpoint, qdrantApiKey,
                embeddingModel, embeddingEndpoint, embeddingApiKey, embeddingDimension);

            return RedirectToAction("Login", "Auth");
        }
    }
}
