# JD.SemanticKernel.Extensions

[![CI](https://github.com/JerrettDavis/JD.SemanticKernel.Extensions/actions/workflows/ci.yml/badge.svg)](https://github.com/JerrettDavis/JD.SemanticKernel.Extensions/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/JD.SemanticKernel.Extensions.svg)](https://www.nuget.org/packages/JD.SemanticKernel.Extensions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

An extensible toolkit for [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel) that bridges **Claude Code skills, plugins, and hooks** into SK applications, and adds **context management primitives** (compaction, semantic memory) for building production-grade AI agents.

## Features

- 📝 **Skills** — Parse `SKILL.md` files (YAML frontmatter + markdown) into `KernelFunction` or `PromptTemplate`
- 🔗 **Hooks** — Map Claude Code lifecycle events (`PreToolUse`, `PostToolUse`, etc.) to SK's `IFunctionInvocationFilter` and `IPromptRenderFilter`
- 📦 **Plugins** — Load `.claude-plugin/` directories with skills, hooks, and MCP configs
- 🗜️ **Compaction** — Transparent context window management with configurable triggers and hierarchical summarization
- 🧠 **Memory** — Semantic memory with MMR reranking, temporal decay scoring, and query expansion
- 💾 **Memory.Sqlite** — SQLite-backed persistent memory storage
- 🎯 **Fluent API** — `UseSkills()`, `UseHooks()`, `UsePlugins()`, `AddCompaction()`, `AddSemanticMemory()` extension methods

## Packages

| Package | Description | NuGet |
|---|---|---|
| `JD.SemanticKernel.Extensions.Skills` | SKILL.md → KernelFunction/PromptTemplate | [![NuGet](https://img.shields.io/nuget/v/JD.SemanticKernel.Extensions.Skills.svg)](https://www.nuget.org/packages/JD.SemanticKernel.Extensions.Skills) |
| `JD.SemanticKernel.Extensions.Hooks` | Claude Code hooks → SK filters | [![NuGet](https://img.shields.io/nuget/v/JD.SemanticKernel.Extensions.Hooks.svg)](https://www.nuget.org/packages/JD.SemanticKernel.Extensions.Hooks) |
| `JD.SemanticKernel.Extensions.Plugins` | Plugin directory loader | [![NuGet](https://img.shields.io/nuget/v/JD.SemanticKernel.Extensions.Plugins.svg)](https://www.nuget.org/packages/JD.SemanticKernel.Extensions.Plugins) |
| `JD.SemanticKernel.Extensions.Compaction` | Context window compaction middleware | [![NuGet](https://img.shields.io/nuget/v/JD.SemanticKernel.Extensions.Compaction.svg)](https://www.nuget.org/packages/JD.SemanticKernel.Extensions.Compaction) |
| `JD.SemanticKernel.Extensions.Memory` | Semantic memory with MMR + temporal decay | [![NuGet](https://img.shields.io/nuget/v/JD.SemanticKernel.Extensions.Memory.svg)](https://www.nuget.org/packages/JD.SemanticKernel.Extensions.Memory) |
| `JD.SemanticKernel.Extensions.Memory.Sqlite` | SQLite memory backend | [![NuGet](https://img.shields.io/nuget/v/JD.SemanticKernel.Extensions.Memory.Sqlite.svg)](https://www.nuget.org/packages/JD.SemanticKernel.Extensions.Memory.Sqlite) |
| `JD.SemanticKernel.Extensions` | Meta-package (all of the above) | [![NuGet](https://img.shields.io/nuget/v/JD.SemanticKernel.Extensions.svg)](https://www.nuget.org/packages/JD.SemanticKernel.Extensions) |

## Quick Start

```bash
dotnet add package JD.SemanticKernel.Extensions
```

### Load Skills

```csharp
using JD.SemanticKernel.Extensions.Skills;

var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-4o", apiKey)
    .UseSkills("./skills/")   // Scans for SKILL.md files
    .Build();
```

A `SKILL.md` file follows the [Claude Code / AgentSkills.io](https://agentskills.io) format:

```markdown
---
name: code-reviewer
description: Reviews code for quality issues
allowed-tools: [Read, Grep, Glob]
---
# Code Reviewer

Review the provided code for:
1. Bug risks
2. Security vulnerabilities
3. Performance issues

Input: $ARGUMENTS
```

### Configure Hooks

```csharp
using JD.SemanticKernel.Extensions.Hooks;

var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-4o", apiKey)
    .UseHooks(hooks =>
    {
        hooks.OnFunctionInvoking("Bash|Execute", async ctx =>
        {
            Console.WriteLine($"Validating: {ctx.Function.Name}");
        });
        hooks.OnFunctionInvoked("Write|Edit", async ctx =>
        {
            Console.WriteLine($"Post-edit hook: {ctx.Function.Name}");
        });
        hooks.OnPromptRendering(async ctx =>
        {
            Console.WriteLine("Prompt is about to render...");
        });
    })
    .Build();
```

### Load Plugins

```csharp
using JD.SemanticKernel.Extensions.Plugins;

var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-4o", apiKey)
    .UsePlugins("./my-plugin/")         // Single plugin directory
    .UseAllPlugins("./plugins/")        // All plugins in directory
    .Build();
```

Plugin directories follow the `.claude-plugin/` convention:

```
my-plugin/
├── .claude-plugin/
│   └── plugin.json          # Manifest
├── skills/
│   └── reviewer/SKILL.md    # Skills
├── hooks/
│   └── hooks.json           # Hooks
└── .mcp.json                # MCP servers (future)
```

### Meta-Package (All-in-One)

```csharp
using JD.SemanticKernel.Extensions;

var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-4o", apiKey)
    .AddClaudeCodeSkills("./skills/")
    .AddClaudeCodePlugin("./my-plugin/")
    .AddClaudeCodeHooks(hooks => hooks.OnFunctionInvoking(".*", _ => Task.CompletedTask))
    .Build();
```

### Context Compaction

Automatically compress chat history when it grows too large, preserving key context while staying within token limits.

```csharp
using JD.SemanticKernel.Extensions.Compaction;

var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-4o", apiKey)
    .Build();

// Register compaction as transparent middleware
kernel.Services.AddCompaction(options =>
{
    options.TriggerMode = CompactionTriggerMode.ContextPercentage;
    options.Threshold = 0.70;                  // Compact at 70% of context window
    options.MaxContextWindowTokens = 128_000;  // Model's context limit
    options.PreserveLastMessages = 10;         // Always keep recent messages
    options.MinMessagesBeforeCompaction = 5;   // Don't compact short conversations
});
```

**Trigger modes:**
- `TokenThreshold` — Compact when estimated tokens exceed an absolute count
- `ContextPercentage` — Compact when usage exceeds a percentage of the context window

**Token estimation:**

```csharp
var tokens = TokenEstimator.EstimateTokens("Hello world");          // ~2 tokens
var historyTokens = TokenEstimator.EstimateTokens(chatHistory);     // Includes overhead
```

### Semantic Memory

Store, search, and retrieve context-relevant information with embedding-based similarity, MMR diversity reranking, and temporal decay scoring.

```csharp
using JD.SemanticKernel.Extensions.Memory;

// Register with in-memory backend
kernel.Services.AddSemanticMemory(options =>
{
    options.DefaultSearchOptions = new MemorySearchOptions
    {
        TopK = 10,
        MinRelevanceScore = 0.7,
        UseMmrReranking = true,
        MmrLambda = 0.7,           // Balance relevance vs diversity
        UseTemporalDecay = true,
        TemporalDecayRate = 0.01,
    };
});
```

**SQLite persistence:**

```csharp
using JD.SemanticKernel.Extensions.Memory.Sqlite;

kernel.Services.AddSqliteMemoryBackend("Data Source=memory.db");
```

**Key capabilities:**
- **MMR reranking** — Maximal Marginal Relevance for diverse search results
- **Temporal decay** — Recent memories rank higher with configurable decay rate
- **Query expansion** — Automatically generate alternative queries for broader recall
- **Pluggable backends** — `InMemoryBackend` (default), `SqliteMemoryBackend`, or implement `IMemoryBackend`

## Hook Event Mapping

| Claude Code Event | SK Filter |
|---|---|
| `PreToolUse` | `IFunctionInvocationFilter.OnFunctionInvokingAsync` |
| `PostToolUse` | `IFunctionInvocationFilter.OnFunctionInvokedAsync` |
| `UserPromptSubmit` | `IPromptRenderFilter.OnPromptRenderingAsync` |
| `Stop` / `SubagentStop` | `IAutoFunctionInvocationFilter` |
| `SessionStart` / `SessionEnd` | `IExtensionEventBus` (custom) |
| `PreCompact` / `Notification` | `IExtensionEventBus` (custom) |

## Related Projects

| Project | Description |
|---|---|
| [JD.SemanticKernel.Connectors.ClaudeCode](https://github.com/JerrettDavis/JD.SemanticKernel.Connectors.ClaudeCode) | Claude Code authentication provider for SK |
| [JD.SemanticKernel.Connectors.GitHubCopilot](https://github.com/JerrettDavis/JD.SemanticKernel.Connectors.GitHubCopilot) | GitHub Copilot authentication provider for SK |

## Building

```bash
dotnet restore
dotnet build
dotnet test
```

## License

[MIT](LICENSE) © JD
