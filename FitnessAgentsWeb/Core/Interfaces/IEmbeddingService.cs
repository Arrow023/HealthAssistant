using System.Threading.Tasks;

namespace FitnessAgentsWeb.Core.Interfaces
{
    /// <summary>
    /// Generates vector embeddings from text for semantic similarity search.
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generates a float embedding vector from the given text.
        /// </summary>
        Task<float[]> GenerateEmbeddingAsync(string text);
    }
}
