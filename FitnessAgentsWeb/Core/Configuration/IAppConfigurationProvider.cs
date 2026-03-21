namespace FitnessAgentsWeb.Core.Configuration
{
    public interface IAppConfigurationProvider
    {
        string GetAiModel();
        string GetAiKey();
        string GetAiEndpoint();
        string GetOcrModel();
        string GetOcrKey();
        string GetOcrEndpoint();
        string GetSmtpPassword();
        string GetSmtpHost();
        string GetSmtpPort();
        string GetFromEmail();
        string GetToEmail();
        string GetAdminEmail();
        string GetAdminPassword();
        string GetAppTimezone();
        string GetFirebaseDatabaseSecret();
        string GetQdrantEndpoint();
        string GetQdrantApiKey();
        string GetEmbeddingModel();
        string GetEmbeddingEndpoint();
        string GetEmbeddingApiKey();
        string GetEmbeddingDimension();
    }
}
