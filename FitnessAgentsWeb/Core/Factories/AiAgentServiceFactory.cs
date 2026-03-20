using FitnessAgentsWeb.Core.Configuration;
using FitnessAgentsWeb.Core.Interfaces;
using FitnessAgentsWeb.Core.Services;
using Microsoft.Extensions.Configuration;

namespace FitnessAgentsWeb.Core.Factories
{
    public class AiAgentServiceFactory
    {
        private readonly IConfiguration _configuration;
        private readonly IAppConfigurationProvider _configProvider;
        private readonly Microsoft.Extensions.Logging.ILoggerFactory _loggerFactory;

        public AiAgentServiceFactory(IConfiguration configuration, ConfigurationProviderFactory providerFactory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _configProvider = providerFactory.GetProvider();
            _loggerFactory = loggerFactory;
        }

        public IAiAgentService Create()
        {
            string aiType = _configuration["FactorySettings:AiType"] ?? "NVIDIA";

            var logger = _loggerFactory.CreateLogger<NvidiaNimAgentService>();
            if (aiType == "NVIDIA")
            {
                return new NvidiaNimAgentService(_configProvider, logger);
            }

            // Fallback
            return new NvidiaNimAgentService(_configProvider, logger);
        }
    }
}
