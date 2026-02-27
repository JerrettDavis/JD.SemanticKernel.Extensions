using System;
using System.Collections.Generic;
using JD.SemanticKernel.Extensions.Memory;

Console.WriteLine("=== JD.SemanticKernel.Extensions.Memory Demo ===");
Console.WriteLine();

// Demonstrate InMemoryBackend
var backend = new InMemoryBackend();

Console.WriteLine("Storing memories...");
var records = new (string Id, string Text, float[] Embedding)[]
{
    ("auth-1", "JWT authentication uses bearer tokens for stateless auth", new[] { 0.9f, 0.1f, 0.0f }),
    ("auth-2", "OAuth2 provides delegated authorization with access tokens", new[] { 0.85f, 0.15f, 0.0f }),
    ("db-1", "PostgreSQL supports JSONB columns for semi-structured data", new[] { 0.1f, 0.9f, 0.0f }),
    ("db-2", "SQL Server uses clustered indexes for primary key storage", new[] { 0.15f, 0.85f, 0.0f }),
    ("api-1", "REST APIs use HTTP methods to represent CRUD operations", new[] { 0.5f, 0.5f, 0.0f }),
};

foreach (var (id, text, embedding) in records)
{
    await backend.StoreAsync(new MemoryRecord
    {
        Id = id,
        Text = text,
        Embedding = embedding,
        Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["source"] = "demo",
        },
        CreatedAt = DateTimeOffset.UtcNow,
    });
    Console.WriteLine($"  Stored: [{id}] {text}");
}

Console.WriteLine();

// Search for auth-related content
Console.WriteLine("Searching for authentication-related content...");
var queryEmbedding = new float[] { 0.88f, 0.12f, 0.0f };
var results = await backend.SearchAsync(queryEmbedding, topK: 3);

Console.WriteLine($"Top {results.Count} results:");
foreach (var (record, score) in results)
{
    Console.WriteLine($"  [{score:F4}] {record.Id}: {record.Text}");
}

Console.WriteLine();

// Demonstrate MMR reranking
Console.WriteLine("MMR Reranking (lambda=0.5 for balanced relevance+diversity)...");
var mmrResults = MmrReranker.Rerank(results, queryEmbedding, lambda: 0.5, topK: 3);
Console.WriteLine($"MMR Top {mmrResults.Count} results:");
foreach (var (record, score) in mmrResults)
{
    Console.WriteLine($"  [{score:F4}] {record.Id}: {record.Text}");
}

Console.WriteLine();

// Demonstrate temporal decay
Console.WriteLine("Temporal Decay Scoring (half-life = 7 days)...");
var now = DateTimeOffset.UtcNow;
var ages = new[] { 0, 3, 7, 14, 30 };
foreach (var ageDays in ages)
{
    var score = TemporalDecayScorer.ApplyDecay(1.0, now.AddDays(-ageDays), halfLifeDays: 7, now: now);
    Console.WriteLine($"  {ageDays,2} days old: {score:F4} (from 1.0000)");
}

Console.WriteLine();
Console.WriteLine("Note: Full semantic search with embedding generation requires a");
Console.WriteLine("configured ITextEmbeddingGenerationService. Register with:");
Console.WriteLine("  builder.Services.AddSemanticMemory(opt => { ... });");
Console.WriteLine();
Console.WriteLine("Demo complete!");
