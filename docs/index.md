---
_layout: landing
---

# JD.SemanticKernel.Extensions

Bridge Claude Code skills, plugins, and hooks into Microsoft Semantic Kernel.

## Overview

This library parses Claude Code's file-based extension formats (SKILL.md, hooks.json, plugin.json) and registers them as native Semantic Kernel functions, filters, and plugins.

## Packages

| Package | Description |
|---------|-------------|
| `JD.SemanticKernel.Extensions.Skills` | Parse SKILL.md files → SK KernelFunctions |
| `JD.SemanticKernel.Extensions.Hooks` | Parse hooks.json → SK IFunctionInvocationFilter / IPromptRenderFilter |
| `JD.SemanticKernel.Extensions.Plugins` | Load plugin.json manifests with dependency resolution |
| `JD.SemanticKernel.Extensions` | Meta-package with unified `AddClaudeCode*` extensions |

## Quick Start

```csharp
using JD.SemanticKernel.Extensions;

var builder = Kernel.CreateBuilder();

// Load all Claude Code skills from a directory
builder.UseSkills("/path/to/.claude/skills");

// Load hooks as SK filters
builder.UseHooks("/path/to/hooks.json");

// Or load entire plugins
builder.AddClaudeCodePlugin("/path/to/.claude-plugin");
```

## Getting Started

- [Articles](articles/) — Guides and deep-dives
- [Samples](samples/) — Working examples
- [API Reference](api/) — Full API documentation
