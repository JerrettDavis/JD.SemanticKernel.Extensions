# Getting Started

## Installation

Install the meta-package to get all components:

```bash
dotnet add package JD.SemanticKernel.Extensions
```

Or install individual packages:

```bash
dotnet add package JD.SemanticKernel.Extensions.Skills
dotnet add package JD.SemanticKernel.Extensions.Hooks
dotnet add package JD.SemanticKernel.Extensions.Plugins
dotnet add package JD.SemanticKernel.Extensions.Compaction
dotnet add package JD.SemanticKernel.Extensions.Memory
dotnet add package JD.SemanticKernel.Extensions.Memory.Sqlite  # Optional: SQLite backend
```

## Loading Skills

Claude Code skills are SKILL.md files with YAML frontmatter:

```csharp
var builder = Kernel.CreateBuilder();
builder.UseSkills("/path/to/.claude/skills");
var kernel = builder.Build();
```

## Loading Hooks

Hooks map Claude Code lifecycle events to SK filters:

| Claude Code Event | SK Filter |
|---|---|
| PreToolUse | IFunctionInvocationFilter (before) |
| PostToolUse | IFunctionInvocationFilter (after) |
| UserPromptSubmit | IPromptRenderFilter |
| PreCompact, Notification, etc. | IExtensionEventBus |

```csharp
builder.UseHooks("/path/to/hooks.json");
```

## Loading Plugins

Plugins combine skills and hooks into a single manifest:

```csharp
builder.AddClaudeCodePlugin("/path/to/.claude-plugin");
```

## Context Compaction

Register compaction as transparent middleware that automatically summarizes old messages when the context window gets too large:

```csharp
using JD.SemanticKernel.Extensions.Compaction;

kernel.Services.AddCompaction(options =>
{
    options.TriggerMode = CompactionTriggerMode.ContextPercentage;
    options.Threshold = 0.70;
    options.MaxContextWindowTokens = 128_000;
    options.PreserveLastMessages = 10;
});
```

The `CompactionFilter` registers as an `IAutoFunctionInvocationFilter`, so it runs transparently on every function invocation — no code changes needed after registration.

## Semantic Memory

Store and retrieve context-relevant information using embedding-based similarity search:

```csharp
using JD.SemanticKernel.Extensions.Memory;

kernel.Services.AddSemanticMemory(options =>
{
    options.DefaultSearchOptions = new MemorySearchOptions
    {
        TopK = 10,
        MinRelevanceScore = 0.7,
        UseMmrReranking = true,
    };
});

// Store a memory
var memory = kernel.Services.GetRequiredService<ISemanticMemory>();
await memory.StoreAsync("key-1", "Important context about the project");

// Search for relevant memories
var results = await memory.SearchAsync("project context");
```

### SQLite Persistence

For production use, add the SQLite backend:

```csharp
using JD.SemanticKernel.Extensions.Memory.Sqlite;

kernel.Services.AddSqliteMemoryBackend("Data Source=memory.db");
```
