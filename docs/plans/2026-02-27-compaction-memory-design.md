# Extensions.Compaction + Extensions.Memory — Design Document

**Date:** 2026-02-27
**Status:** Approved
**Goal:** Add context management primitives (compaction and semantic memory) to the JD.SemanticKernel.Extensions framework, bringing feature parity with OpenClaw's memory module and Claude Code's PreCompact hook.

## Motivation

Long-running SK conversations exceed model context windows. Developers need transparent context management that works with any model and provider. Additionally, agents benefit from persistent semantic memory that survives across sessions.

## Extensions.Compaction

### Architecture

Transparent middleware that monitors chat history size and compresses older context using hierarchical summarization.

### Core Components

| Component | Type | Purpose |
|---|---|---|
| `ICompactionStrategy` | Interface | Defines how context is compressed |
| `ICompactionTrigger` | Interface | Defines when compaction fires |
| `CompactionFilter` | `IAutoFunctionInvocationFilter` | Transparent SK pipeline middleware |
| `CompactionOptions` | POCO | Configuration (thresholds, preservation rules) |
| `HierarchicalSummarizationStrategy` | Implementation | Progressive summarization of older messages |
| `TokenThresholdTrigger` | Implementation | Fires at absolute token count |
| `ContextPercentageTrigger` | Implementation | Fires at % of model context window |

### Trigger Modes

- **Token threshold:** Compact when estimated tokens exceed N (e.g., 100K)
- **Context percentage:** Compact when usage exceeds X% of model's context window (e.g., 70%)

### Hierarchical Summarization Strategy

1. Messages are grouped into time-based or count-based segments
2. Oldest segments are summarized first using a configurable summary model
3. Summary replaces the original messages, preserving system messages and recent context
4. Progressive: older summaries get re-summarized as new content pushes them further back
5. Key decisions, code snippets, and structured data are preserved with higher priority

### Registration API

```csharp
builder.Services.AddCompaction(options => {
    options.TriggerMode = CompactionTriggerMode.ContextPercentage;
    options.Threshold = 0.70;
    options.PreserveLastMessages = 10;
    options.SummaryModelId = "gpt-4o-mini"; // cheap model for summaries
});
```

### Integration Points

- `PreCompact` hook from Extensions.Hooks triggers automatically
- Works with any `IChatCompletionService` registered in SK
- Compatible with Extensions.Memory (can store pre-compaction context in memory)

## Extensions.Memory

### Architecture

Persistent, semantically-searchable memory built on SK's `ITextEmbeddingGenerationService`, extended with MMR diversity, temporal decay, query expansion, and batch operations.

### Core Components

| Component | Type | Purpose |
|---|---|---|
| `ISemanticMemory` | Interface | Primary memory API (store, search, forget, sync) |
| `IMemoryBackend` | Interface | Storage abstraction (pluggable backends) |
| `MemorySearchOptions` | POCO | Rich query options (MMR, decay, expansion, filters) |
| `MemoryResult` | Record | Search result with score, temporal relevance, metadata |
| `MmrReranker` | Service | Maximal Marginal Relevance for diverse results |
| `TemporalDecayScorer` | Service | Time-based relevance adjustment |
| `QueryExpander` | Service | Automatic query expansion for better recall |
| `InMemoryBackend` | Implementation | Zero-dependency backend for tests/demos |

### Search Features (OpenClaw parity)

- **MMR (Maximal Marginal Relevance):** Balances relevance with diversity. Lambda parameter (0.0 = all diversity, 1.0 = all relevance). Prevents N near-duplicate results.
- **Temporal Decay:** Adjusts relevance scores based on age. Configurable half-life (e.g., 50% weight loss every 7 days).
- **Query Expansion:** Auto-expands queries with related terms for better recall. Uses the registered LLM to generate expansions.
- **Metadata Filters:** Key-value filters on stored metadata for scoped searches.
- **Batch Operations:** Bulk store/embed operations for efficiency.

### Memory Backend Interface

```csharp
public interface IMemoryBackend
{
    Task StoreAsync(string id, ReadOnlyMemory<float> embedding, 
                    IDictionary<string, string> metadata, CancellationToken ct);
    Task<IReadOnlyList<MemoryRecord>> SearchAsync(ReadOnlyMemory<float> query, 
                    int topK, CancellationToken ct);
    Task DeleteAsync(string id, CancellationToken ct);
    Task<bool> ExistsAsync(string id, CancellationToken ct);
}
```

### Registration API

```csharp
builder.Services.AddSemanticMemory(options => {
    options.DefaultSearchOptions.UseMmr = true;
    options.DefaultSearchOptions.MmrLambda = 0.7;
    options.DefaultSearchOptions.TemporalDecayHalfLifeDays = 7;
});

// Register backend separately
builder.Services.AddSqliteMemoryBackend("memory.db");
```

## Package Structure

```
New packages:
├── JD.SemanticKernel.Extensions.Compaction       (SK Abstractions only)
├── JD.SemanticKernel.Extensions.Memory           (SK Abstractions only)  
├── JD.SemanticKernel.Extensions.Memory.Sqlite    (Memory + Microsoft.Data.Sqlite)
└── JD.SemanticKernel.Extensions                  (meta-package, updated)

New test projects:
├── JD.SemanticKernel.Extensions.Compaction.Tests
├── JD.SemanticKernel.Extensions.Memory.Tests
└── JD.SemanticKernel.Extensions.Memory.Sqlite.Tests

New samples:
├── CompactionDemo
└── MemoryDemo
```

### Target Frameworks

- Libraries: `netstandard2.0;net8.0`
- Tests/Samples: `net10.0`

### Dependencies

- **Compaction:** `Microsoft.SemanticKernel.Abstractions` only
- **Memory:** `Microsoft.SemanticKernel.Abstractions` only
- **Memory.Sqlite:** `Extensions.Memory` + `Microsoft.Data.Sqlite`

## Future Tier 2 Extensions (planned)

- `Extensions.Sessions` — Session/conversation state management
- `Extensions.Routing` — Model routing (cost/capability-based provider selection)
- `Extensions.Channels` — Channel abstraction (Slack, Discord, Teams, etc.)
- `Extensions.Cron` — Scheduled/proactive agent tasks
