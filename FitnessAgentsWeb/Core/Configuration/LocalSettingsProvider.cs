using Microsoft.Extensions.Configuration;

namespace FitnessAgentsWeb.Core.Configuration
{
    public class LocalSettingsProvider : IAppConfigurationProvider
    {
        private readonly IConfiguration _configuration;

        public LocalSettingsProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetAiModel() => _configuration["AiSettings:Model"] ?? string.Empty;
        public string GetAiKey() => _configuration["AiSettings:ApiKey"] ?? string.Empty;
        public string GetAiEndpoint() => _configuration["AiSettings:Endpoint"] ?? string.Empty;
        
        public string GetOcrModel() => _configuration["OcrSettings:Model"] ?? string.Empty;
        public string GetOcrKey() => _configuration["OcrSettings:ApiKey"] ?? string.Empty;
        public string GetOcrEndpoint() => _configuration["OcrSettings:Endpoint"] ?? string.Empty;

        public string GetSmtpPassword() => _configuration["SMTP:AppPassword"] ?? string.Empty;
        public string GetSmtpHost() => _configuration["SMTP:Host"] ?? string.Empty;
        public string GetSmtpPort() => _configuration["SMTP:Port"] ?? string.Empty;
        public string GetFromEmail() => _configuration["SMTP:FromEmail"] ?? string.Empty;
        public string GetToEmail() => _configuration["SMTP:ToEmail"] ?? string.Empty;
        
        public string GetAdminEmail() => _configuration["Admin:Email"] ?? string.Empty;
        public string GetAdminPassword() => _configuration["Admin:Password"] ?? string.Empty;
        public string GetAppTimezone() => _configuration["Regional:Timezone"] ?? "India Standard Time";
        public string GetFirebaseDatabaseSecret() => _configuration["FirebaseSettings:DatabaseSecret"] ?? string.Empty;
        public string GetQdrantEndpoint() => _configuration["Qdrant:Endpoint"] ?? string.Empty;
        public string GetQdrantApiKey() => _configuration["Qdrant:ApiKey"] ?? string.Empty;
        public string GetEmbeddingModel() => _configuration["Embedding:Model"] ?? string.Empty;
        public string GetEmbeddingEndpoint() => _configuration["Embedding:Endpoint"] ?? string.Empty;
        public string GetEmbeddingApiKey() => _configuration["Embedding:ApiKey"] ?? string.Empty;
        public string GetEmbeddingDimension() => _configuration["Embedding:Dimension"] ?? string.Empty;
        
        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(GetAiKey()) && !string.IsNullOrEmpty(GetSmtpPassword());
        }
    }
}
