# Architecture

## Package Structure

```
JD.SemanticKernel.Extensions (meta-package)
├── JD.SemanticKernel.Extensions.Skills
│   ├── SkillParser — YAML frontmatter + markdown body parser
│   ├── SkillLoader — Directory scanner for SKILL.md files
│   └── SkillKernelFunction — Adapts skills → KernelFunction/KernelPlugin
├── JD.SemanticKernel.Extensions.Hooks
│   ├── HookParser — JSON parser for hooks.json
│   ├── SkHookFilter — IFunctionInvocationFilter with regex matching
│   ├── SkPromptHookFilter — IPromptRenderFilter
│   ├── HookBuilder — Fluent builder for custom hooks
│   └── ExtensionEventBus — Custom lifecycle event bus
├── JD.SemanticKernel.Extensions.Plugins
│   ├── PluginManifest — plugin.json model
│   ├── PluginLoader — Directory scanner + orchestrator
│   └── PluginDependencyResolver — Topological sort
├── JD.SemanticKernel.Extensions.Compaction
│   ├── CompactionFilter — IAutoFunctionInvocationFilter middleware
│   ├── TokenEstimator — Character-based token estimation
│   ├── TokenThresholdTrigger — Absolute token count trigger
│   ├── ContextPercentageTrigger — Context window percentage trigger
│   └── HierarchicalSummarizationStrategy — Progressive message summarization
├── JD.SemanticKernel.Extensions.Memory
│   ├── SemanticMemory — Search pipeline orchestrator
│   ├── InMemoryBackend — Default in-process storage
│   ├── MmrReranker — Maximal Marginal Relevance diversity
│   ├── TemporalDecayScorer — Time-weighted relevance
│   └── QueryExpander — Alternative query generation
└── JD.SemanticKernel.Extensions.Memory.Sqlite
    └── SqliteMemoryBackend — Persistent storage with blob embeddings
```

## Target Frameworks

All packages target `netstandard2.0` and `net8.0` for maximum compatibility.
Test projects and samples target `net10.0`.

## Claude Code Format Mapping

### SKILL.md → KernelFunction

The YAML frontmatter maps to function metadata. The markdown body becomes the prompt template. `$ARGUMENTS` maps to `{{$input}}`, positional args `$0`, `$1` map to `{{$arg0}}`, `{{$arg1}}`.

### hooks.json → SK Filters

`PreToolUse` and `PostToolUse` events become `IFunctionInvocationFilter` instances. Tool name patterns are matched via regex. Events without direct SK equivalents (SessionStart, PreCompact, etc.) are routed through `IExtensionEventBus`.

### plugin.json → KernelPlugin

The plugin manifest orchestrates loading skills and hooks from subdirectories. Dependencies are resolved via topological sort before loading.

## Compaction Architecture

The compaction pipeline operates as transparent middleware:

1. **Trigger evaluation** — On each function invocation, the registered `ICompactionTrigger` checks whether the chat history exceeds the configured threshold
2. **Strategy execution** — If triggered, the `ICompactionStrategy` (default: `HierarchicalSummarizationStrategy`) summarizes older messages while preserving system messages and the most recent N messages
3. **History replacement** — The compacted history replaces the original, staying within token limits

The `CompactionFilter` registers as an `IAutoFunctionInvocationFilter`, requiring zero code changes after initial configuration.

## Memory Architecture

The semantic memory system follows a pipeline pattern:

1. **Store** — Text is embedded via SK's `ITextEmbeddingGenerationService` and stored in the backend
2. **Search** — Query text is embedded, then candidates are retrieved by cosine similarity
3. **Filter** — Results below `MinRelevanceScore` are discarded
4. **Rerank** — MMR reranking balances relevance against diversity (configurable λ)
5. **Decay** — Temporal decay adjusts scores based on last access time
6. **Return** — Top-K results with scores and metadata

The `IMemoryBackend` interface is pluggable: `InMemoryBackend` ships in the core package for development, while `SqliteMemoryBackend` provides persistent storage without requiring native vector database extensions.
