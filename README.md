# JD.SemanticKernel.Extensions

[![CI](https://github.com/JerrettDavis/JD.SemanticKernel.Extensions/actions/workflows/ci.yml/badge.svg)](https://github.com/JerrettDavis/JD.SemanticKernel.Extensions/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/JD.SemanticKernel.Extensions.svg)](https://www.nuget.org/packages/JD.SemanticKernel.Extensions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Bridge **Claude Code skills, plugins, and hooks** into [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel) applications. Load file-based SKILL.md definitions, lifecycle hooks, and plugin packages as native SK components.

## Features

- 📝 **Skills** — Parse `SKILL.md` files (YAML frontmatter + markdown) into `KernelFunction` or `PromptTemplate`
- 🔗 **Hooks** — Map Claude Code lifecycle events (`PreToolUse`, `PostToolUse`, etc.) to SK's `IFunctionInvocationFilter` and `IPromptRenderFilter`
- 📦 **Plugins** — Load `.claude-plugin/` directories with skills, hooks, and MCP configs
- 🎯 **Fluent API** — `UseSkills()`, `UseHooks()`, `UsePlugins()` extension methods on `IKernelBuilder`

## Packages

| Package | Description | NuGet |
|---|---|---|
| `JD.SemanticKernel.Extensions.Skills` | SKILL.md → KernelFunction/PromptTemplate | [![NuGet](https://img.shields.io/nuget/v/JD.SemanticKernel.Extensions.Skills.svg)](https://www.nuget.org/packages/JD.SemanticKernel.Extensions.Skills) |
| `JD.SemanticKernel.Extensions.Hooks` | Claude Code hooks → SK filters | [![NuGet](https://img.shields.io/nuget/v/JD.SemanticKernel.Extensions.Hooks.svg)](https://www.nuget.org/packages/JD.SemanticKernel.Extensions.Hooks) |
| `JD.SemanticKernel.Extensions.Plugins` | Plugin directory loader | [![NuGet](https://img.shields.io/nuget/v/JD.SemanticKernel.Extensions.Plugins.svg)](https://www.nuget.org/packages/JD.SemanticKernel.Extensions.Plugins) |
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
