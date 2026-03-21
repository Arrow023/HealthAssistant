using FitnessAgentsWeb.Core.Configuration;
using FitnessAgentsWeb.Core.Interfaces;
using FitnessAgentsWeb.Models;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FitnessAgentsWeb.Core.Services
{
    /// <summary>
    /// Qdrant-backed vector store for plan records.
    /// Stores plan embeddings with metadata for filtered semantic search.
    /// </summary>
    public class QdrantPlanVectorStore : IPlanVectorStore
    {
        private const string CollectionName = "health_plans";
        private const ulong DefaultVectorSize = 1536;

        private readonly IAppConfigurationProvider _configProvider;
        private readonly ILogger<QdrantPlanVectorStore> _logger;

        public QdrantPlanVectorStore(
            IAppConfigurationProvider configProvider,
            ILogger<QdrantPlanVectorStore> logger)
        {
            _configProvider = configProvider;
            _logger = logger;
        }

        private QdrantClient CreateClient()
        {
            string endpoint = _configProvider.GetQdrantEndpoint();
            string apiKey = _configProvider.GetQdrantApiKey();

            if (string.IsNullOrEmpty(endpoint))
            {
                _logger.LogWarning("[QdrantVectorStore] No Qdrant endpoint configured; using localhost default");
                endpoint = "http://localhost:6334";
            }

            var uri = new Uri(endpoint);
            bool useTls = uri.Scheme == "https";

            // For plain HTTP (no TLS), enable HTTP/2 cleartext (h2c) to satisfy gRPC requirements
            if (!useTls)
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            }

            // For Qdrant Cloud (HTTPS without explicit port), use port 6334 for gRPC
            int port = uri.IsDefaultPort ? 6334 : uri.Port;
            var grpcAddress = new Uri($"{uri.Scheme}://{uri.Host}:{port}");

            if (!string.IsNullOrEmpty(apiKey))
            {
                return new QdrantClient(grpcAddress, apiKey);
            }

            return new QdrantClient(grpcAddress);
        }

        private ulong GetConfiguredVectorSize()
        {
            string dimStr = _configProvider.GetEmbeddingDimension();
            if (!string.IsNullOrEmpty(dimStr) && ulong.TryParse(dimStr, out ulong dim) && dim > 0)
                return dim;
            return DefaultVectorSize;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var client = CreateClient();
                var vectorSize = GetConfiguredVectorSize();
                var collections = await client.ListCollectionsAsync();

                bool collectionCreated = false;

                if (!collections.Any(c => c == CollectionName))
                {
                    await client.CreateCollectionAsync(CollectionName, new VectorParams
                    {
                        Size = vectorSize,
                        Distance = Distance.Cosine
                    });
                    collectionCreated = true;

                    _logger.LogInformation("[QdrantVectorStore] Created collection '{Collection}' ({Size}-dim, cosine)", CollectionName, vectorSize);
                }
                else
                {
                    // Verify existing collection has the correct vector dimension
                    var collectionInfo = await client.GetCollectionInfoAsync(CollectionName);
                    var existingSize = collectionInfo.Config?.Params?.VectorsConfig?.Params?.Size ?? 0;

                    if (existingSize > 0 && existingSize != vectorSize)
                    {
                        _logger.LogWarning("[QdrantVectorStore] Collection dimension mismatch (existing={Existing}, configured={Configured}). Recreating collection.",
                            existingSize, vectorSize);
                        await client.DeleteCollectionAsync(CollectionName);
                        await client.CreateCollectionAsync(CollectionName, new VectorParams
                        {
                            Size = vectorSize,
                            Distance = Distance.Cosine
                        });
                        collectionCreated = true;
                        _logger.LogInformation("[QdrantVectorStore] Recreated collection '{Collection}' ({Size}-dim, cosine)", CollectionName, vectorSize);
                    }
                    else
                    {
                        _logger.LogInformation("[QdrantVectorStore] Collection '{Collection}' already exists ({Size}-dim)", CollectionName, existingSize);
                    }
                }

                // Ensure payload indexes exist for filtered search (idempotent — safe to call even if they already exist)
                await client.CreatePayloadIndexAsync(CollectionName, "user_id", PayloadSchemaType.Keyword);
                await client.CreatePayloadIndexAsync(CollectionName, "plan_type", PayloadSchemaType.Keyword);
                _logger.LogInformation("[QdrantVectorStore] Payload indexes ensured for user_id, plan_type");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[QdrantVectorStore] Failed to initialize — vector features will be unavailable until Qdrant is reachable");
            }
        }

        public async Task UpsertAsync(string userId, PlanRecord record, float[] embedding)
        {
            if (embedding.Length == 0)
            {
                _logger.LogWarning("[QdrantVectorStore] Empty embedding for plan {PlanId}, skipping upsert", record.Id);
                return;
            }

            try
            {
                var client = CreateClient();

                // Deterministic ID from the plan ID string
                var pointId = GeneratePointId(record.Id);

                var payload = new Dictionary<string, Value>
                {
                    ["user_id"] = userId,
                    ["plan_type"] = record.PlanType,
                    ["muscle_group"] = record.MuscleGroup,
                    ["plan_date"] = record.PlanDate.ToString("o"),
                    ["recovery_score"] = record.RecoveryScore,
                    ["sleep_score"] = record.SleepScore,
                    ["active_score"] = record.ActiveScore,
                    ["plan_summary"] = record.PlanSummary,
                    ["plan_json"] = record.PlanJson,
                    ["rhr"] = record.Rhr,
                    ["hrv"] = record.Hrv,
                    ["sleep_total"] = record.SleepTotal,
                    ["rating"] = record.Feedback?.Rating ?? 0,
                    ["difficulty"] = record.Feedback?.Difficulty ?? "",
                    ["feedback_note"] = record.Feedback?.Note ?? "",
                    ["skipped_items"] = record.Feedback?.SkippedItems is not null
                        ? string.Join(",", record.Feedback.SkippedItems)
                        : ""
                };

                var point = new PointStruct
                {
                    Id = pointId,
                    Vectors = embedding,
                    Payload = { }
                };

                foreach (var kvp in payload)
                {
                    point.Payload[kvp.Key] = kvp.Value;
                }

                await client.UpsertAsync(CollectionName, [point]);

                _logger.LogInformation("[QdrantVectorStore] Upserted plan {PlanId} for user {UserId}", record.Id, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[QdrantVectorStore] Failed to upsert plan {PlanId}", record.Id);
            }
        }

        public async Task<List<PlanSearchResult>> SearchSimilarAsync(
            string userId,
            string planType,
            float[] queryEmbedding,
            int topK = 3,
            float minScore = 0.65f)
        {
            var results = new List<PlanSearchResult>();

            if (queryEmbedding.Length == 0)
            {
                _logger.LogWarning("[QdrantVectorStore] Empty query embedding, returning no results");
                return results;
            }

            try
            {
                var client = CreateClient();

                // Filter by user and plan type
                var filter = new Filter
                {
                    Must =
                    {
                        new Condition { Field = new FieldCondition
                        {
                            Key = "user_id",
                            Match = new Match { Keyword = userId }
                        }},
                        new Condition { Field = new FieldCondition
                        {
                            Key = "plan_type",
                            Match = new Match { Keyword = planType }
                        }}
                    }
                };

                var searchResult = await client.SearchAsync(
                    CollectionName,
                    queryEmbedding,
                    filter: filter,
                    limit: (ulong)topK,
                    scoreThreshold: minScore);

                foreach (var hit in searchResult)
                {
                    var record = new PlanRecord
                    {
                        Id = hit.Id.Num.ToString(),
                        UserId = GetPayloadString(hit.Payload, "user_id"),
                        PlanType = GetPayloadString(hit.Payload, "plan_type"),
                        MuscleGroup = GetPayloadString(hit.Payload, "muscle_group"),
                        PlanSummary = GetPayloadString(hit.Payload, "plan_summary"),
                        PlanJson = GetPayloadString(hit.Payload, "plan_json"),
                        Rhr = GetPayloadString(hit.Payload, "rhr"),
                        Hrv = GetPayloadString(hit.Payload, "hrv"),
                        SleepTotal = GetPayloadString(hit.Payload, "sleep_total"),
                        RecoveryScore = GetPayloadInt(hit.Payload, "recovery_score"),
                        SleepScore = GetPayloadInt(hit.Payload, "sleep_score"),
                        ActiveScore = GetPayloadInt(hit.Payload, "active_score"),
                    };

                    if (DateTime.TryParse(GetPayloadString(hit.Payload, "plan_date"), out var planDate))
                        record.PlanDate = planDate;

                    int rating = GetPayloadInt(hit.Payload, "rating");
                    string feedbackNote = GetPayloadString(hit.Payload, "feedback_note");
                    string difficulty = GetPayloadString(hit.Payload, "difficulty");
                    string skipped = GetPayloadString(hit.Payload, "skipped_items");

                    if (rating > 0 || !string.IsNullOrEmpty(feedbackNote))
                    {
                        record.Feedback = new PlanFeedback
                        {
                            Rating = rating,
                            Difficulty = difficulty,
                            Note = feedbackNote,
                            SkippedItems = string.IsNullOrEmpty(skipped)
                                ? []
                                : skipped.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                        };
                    }

                    results.Add(new PlanSearchResult { Record = record, Score = hit.Score });
                }

                _logger.LogInformation("[QdrantVectorStore] Found {Count} similar {PlanType} plans for {UserId}", results.Count, planType, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[QdrantVectorStore] Search failed for user {UserId}", userId);
            }

            return results;
        }

        public async Task AttachFeedbackAsync(string userId, string planId, PlanFeedback feedback)
        {
            try
            {
                var client = CreateClient();
                var pointIdHash = GeneratePointIdHash(planId);

                var payload = new Dictionary<string, Value>
                {
                    ["rating"] = feedback.Rating,
                    ["difficulty"] = feedback.Difficulty,
                    ["feedback_note"] = feedback.Note,
                    ["skipped_items"] = feedback.SkippedItems is not null
                        ? string.Join(",", feedback.SkippedItems)
                        : ""
                };

                await client.SetPayloadAsync(CollectionName, payload, pointIdHash);

                _logger.LogInformation("[QdrantVectorStore] Attached feedback (rating={Rating}) to plan {PlanId}", feedback.Rating, planId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[QdrantVectorStore] Failed to attach feedback to plan {PlanId}", planId);
            }
        }

        private static PointId GeneratePointId(string planId)
        {
            return GeneratePointIdHash(planId);
        }

        private static ulong GeneratePointIdHash(string planId)
        {
            // Use a deterministic FNV-1a hash to generate a ulong point ID from the string plan ID
            ulong hash = 14695981039346656037;
            foreach (byte b in System.Text.Encoding.UTF8.GetBytes(planId))
            {
                hash ^= b;
                hash *= 1099511628211;
            }
            return hash;
        }

        private static string GetPayloadString(IDictionary<string, Value> payload, string key)
        {
            if (payload.TryGetValue(key, out var value) && value.KindCase == Value.KindOneofCase.StringValue)
                return value.StringValue;
            return string.Empty;
        }

        private static int GetPayloadInt(IDictionary<string, Value> payload, string key)
        {
            if (payload.TryGetValue(key, out var value) && value.KindCase == Value.KindOneofCase.IntegerValue)
                return (int)value.IntegerValue;
            return 0;
        }
    }
}
