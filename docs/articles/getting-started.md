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
