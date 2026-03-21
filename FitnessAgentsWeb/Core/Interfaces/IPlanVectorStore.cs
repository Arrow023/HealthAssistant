using FitnessAgentsWeb.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FitnessAgentsWeb.Core.Interfaces
{
    /// <summary>
    /// Vector store for plan records. Supports semantic similarity search
    /// across historical workout/diet plans with their context snapshots.
    /// </summary>
    public interface IPlanVectorStore
    {
        /// <summary>
        /// Initializes the vector store collection/schema if it doesn't exist.
        /// Called once during application startup.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Inserts or updates a plan record with its embedding in the vector store.
        /// </summary>
        Task UpsertAsync(string userId, PlanRecord record, float[] embedding);

        /// <summary>
        /// Searches for similar historical plans based on a query embedding.
        /// Filters by userId and planType; returns top-K results above the minimum score.
        /// </summary>
        Task<List<PlanSearchResult>> SearchSimilarAsync(
            string userId,
            string planType,
            float[] queryEmbedding,
            int topK = 3,
            float minScore = 0.65f);

        /// <summary>
        /// Attaches user feedback to an existing plan record in the vector store.
        /// </summary>
        Task AttachFeedbackAsync(string userId, string planId, PlanFeedback feedback);
    }
}
