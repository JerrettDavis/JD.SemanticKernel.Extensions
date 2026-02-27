# jdai — Semantic Kernel TUI Agent

**Date:** 2026-02-27
**Status:** Approved
**Location:** `samples/JD.AI.Tui/`
**Tool name:** `jdai`

## Overview

A terminal user interface (TUI) that operates like Claude Code, GitHub Copilot CLI, or Codex — but powered by Semantic Kernel and our full extension ecosystem. Users select a provider and model at startup, then interact with an AI agent that has access to developer tools, semantic memory, context compaction, skills, plugins, and hooks.

**Unique advantages over existing CLIs:**
- **Multi-provider:** Runtime switching between Claude Code, GitHub Copilot, and Ollama
- **Semantic memory:** Persistent recall across sessions via our Memory package

## Architecture

```
┌──────────────────────────────────────────┐
│              TUI Shell                    │
│  Terminal.Gui layout + Spectre rendering  │
├──────────────────────────────────────────┤
│              Agent Engine                 │
│  SK ChatCompletion + auto-function-call   │
│  Streaming + compaction + memory          │
├──────────────────────────────────────────┤
│           Provider Registry               │
│  Auto-detect + unified model catalog      │
│  Hot-swap via /model command              │
├──────────────────────────────────────────┤
│            Tool Registry                  │
│  Built-in tools + MCP + Skills/Plugins    │
└──────────────────────────────────────────┘
```

## Provider Registry

### Startup Detection

1. **Claude Code** — Check `~/.claude/.credentials.json` exists + token not expired
2. **GitHub Copilot** — Check `apps.json`/`hosts.json` exists + token exchange succeeds
3. **Ollama** — HTTP GET `http://localhost:11434/api/tags`

### Model Catalog

All detected providers contribute to a unified model catalog:

```csharp
public record ProviderInfo(
    string Name,
    bool IsAvailable,
    string? StatusMessage,
    IReadOnlyList<ModelInfo> Models);
```

### Runtime Switching

`/model <id>` rebuilds the SK Kernel with the new provider's chat completion service. ChatHistory, memory state, and tool registrations carry over — only the LLM backend changes.

## Tool System

### Built-in Tools

| Category | Tool | Description |
|----------|------|-------------|
| **File** | `read_file` | Read file (line range, encoding detection) |
| | `write_file` | Write/create/overwrite file |
| | `edit_file` | Surgical search-and-replace edits |
| | `list_directory` | Recursive listing with depth control |
| **Search** | `grep` | Content search (regex, glob filter, context lines) |
| | `glob` | File pattern matching |
| **Shell** | `run_command` | Execute command (timeout, cwd, env) |
| **Git** | `git_status` | Working tree status |
| | `git_diff` | Diff (staged/unstaged/branch) |
| | `git_commit` | Commit with message |
| | `git_log` | Recent commit history |
| **Web** | `web_fetch` | Fetch URL → markdown or raw HTML |
| | `web_search` | AI-powered web search with citations |
| **MCP** | Dynamic | Tools from connected MCP servers |
| **Memory** | `memory_store` | Store text in semantic memory |
| | `memory_search` | Search semantic memory |
| | `memory_forget` | Remove from memory |

### MCP Server Support

Reads `.mcp.json` (project-level) and `~/.mcp.json` (global) to discover MCP servers. Each server's tools are registered as `KernelFunction`s dynamically via stdio or SSE transport.

```json
{
  "servers": {
    "filesystem": { "command": "npx", "args": ["-y", "@anthropic/mcp-filesystem"] },
    "github": { "url": "https://mcp.github.com/sse", "transport": "sse" }
  }
}
```

### Plugin/Skill Loading

Leverages our existing Extensions packages:
- `.UseSkills("~/.claude/skills/")` — loads SKILL.md → KernelFunctions
- `.UsePlugins("~/.claude/plugins/")` — loads plugin manifests
- `.UseHooks()` — registers lifecycle hooks as SK filters

### Safety Tiers

| Tier | Tools | Behavior |
|------|-------|----------|
| Auto-approve | read_file, list_directory, grep, glob, git_status, git_diff, memory_search, web_fetch | Execute immediately |
| Confirm once | write_file, edit_file, git_commit | Ask first time per session |
| Always confirm | run_command, web_search, MCP tools | Always ask |
| Override | `/autorun on` | Skip all confirmations |

## Slash Commands

| Command | Description |
|---------|-------------|
| `/help` | Show all commands |
| `/model <id>` | Switch model |
| `/models` | List available models |
| `/provider` | Show current provider + auth status |
| `/providers` | List all providers |
| `/clear` | Clear chat history |
| `/compact` | Force compaction now |
| `/memory <query>` | Search semantic memory |
| `/memory store <text>` | Manually store to memory |
| `/plugins` | List loaded plugins/skills |
| `/skills` | List loaded skills |
| `/hooks` | List active hooks |
| `/mcp` | List MCP servers + their tools |
| `/autorun [on\|off]` | Toggle auto-approve for tools |
| `/cost` | Show token usage + estimated cost |
| `/export <file>` | Export chat history to markdown |
| `/config` | Show/edit runtime settings |
| `/quit` | Exit |

Custom commands load from `~/.jdai/commands/` or `.jdai/commands/` (project-level).

## Agent Loop

```
User Input
    │
    ├── Slash Command? ──► Execute Command, Print Result
    │
    └── Chat Message
         │
         ▼
    Pre-prompt hooks
         │
         ▼
    SK ChatCompletion (streaming)
    ◄──► Auto Function Calling ──► Tool Execution (confirm if needed)
         │
         ▼
    Post-response hooks
         │
         ▼
    Render to TUI (markdown → rich text)
         │
         ▼
    Compaction check (if token budget exceeded)
         │
         ▼
    Memory extraction (auto-store key facts/decisions)
```

### Streaming

Responses stream token-by-token into the chat panel. Tool calls show a spinner with tool name during execution. The status bar updates token count in real-time.

### Compaction

After each response, the `CompactionFilter` checks if `ChatHistory` exceeds the token budget. If so, it automatically summarizes older messages using hierarchical summarization before the next turn.

### Memory Extraction

After each assistant response, a lightweight classifier identifies key facts, decisions, or file changes worth remembering. These get auto-stored in semantic memory for future recall.

### Error Handling

- LLM failures: Red panel with retry option
- Tool failures: Error injected into ChatHistory so the agent can self-correct
- Auth expiry: Triggers re-authentication flow

## TUI Layout

```
┌─ jdai ─────────────────────────────────────────────────────┐
│ ┌─ Chat ─────────────────────────────────────────────────┐ │
│ │ 🧑 What files are in the src directory?                │ │
│ │                                                         │ │
│ │ 🤖 I'll list the directory for you.                    │ │
│ │ ┌─ 🔧 list_directory("src/") ────────────────────────┐ │ │
│ │ │ src/                                                │ │ │
│ │ │ ├── Program.cs                                      │ │ │
│ │ │ ├── Services/                                       │ │ │
│ │ │ └── Models/                                         │ │ │
│ │ └────────────────────────────────────────────────────┘ │ │
│ │                                                         │ │
│ │ 🤖 The src directory contains Program.cs and two       │ │
│ │    subdirectories: Services/ and Models/.               │ │
│ └─────────────────────────────────────────────────────────┘ │
│ ┌─ Input ─────────────────────────────────────────────────┐ │
│ │ > _                                                     │ │
│ └─────────────────────────────────────────────────────────┘ │
│ Claude Code │ claude-opus-4-6 │ 1,247 tokens │ /help       │
└─────────────────────────────────────────────────────────────┘
```

- **Chat panel** (scrollable): Markdown → rich text. Tool outputs as bordered sub-panels. Syntax-highlighted code blocks.
- **Input bar**: Multi-line. Enter sends, Shift+Enter newline. Up recalls history. Tab completes slash commands.
- **Status bar**: Provider, model, token count, help hint.
- **Theming**: Provider-specific accents (Claude orange, Copilot blue, Ollama green). Auto-detect dark/light.

## Parity Analysis

| Feature | Claude Code | Copilot | jdai v1 | Status |
|---------|:-----------:|:-------:|:-------:|--------|
| Chat + streaming | ✅ | ✅ | ✅ | SK streaming API |
| File read/write/edit | ✅ | ✅ | ✅ | Native tools |
| Shell execution | ✅ | ✅ | ✅ | With confirmation |
| Code search | ✅ | ✅ | ✅ | grep/glob tools |
| Git operations | ✅ | ✅ | ✅ | status/diff/commit/log |
| Web fetch | ✅ | ❌ | ✅ | HttpClient + HTML→MD |
| MCP servers | ✅ | ❌ | ✅ | stdio + SSE transport |
| Slash commands | ✅ | ✅ | ✅ | Full set |
| Skills/Plugins/Hooks | ✅ | ❌ | ✅ | Extensions packages |
| Context compaction | ✅ | ✅ | ✅ | Compaction package |
| Semantic memory | ❌ | ❌ | ✅ | **Advantage** |
| Multi-provider | ❌ | ❌ | ✅ | **Advantage** |
| Token cost tracking | ✅ | ❌ | ✅ | SK metadata |
| Auto-approve mode | ✅ | ✅ | ✅ | /autorun |
| Sub-agents | ✅ | ❌ | ❌ | v2 |
| Planning mode | ✅ | ❌ | ❌ | v2 |
| Vision input | ✅ | ✅ | ❌ | v2 |
| Notifications | ✅ | ❌ | ❌ | v2 |

**v1 delivers 14/18 features** with two unique advantages.

## Implementation Phases

### Phase 1: Scaffold & Provider Registry
- Create project structure in `samples/JD.AI.Tui/`
- Add NuGet references (Terminal.Gui, Spectre.Console, SK, connectors)
- Implement `IProviderRegistry` with auto-detection
- Provider health check + model catalog

### Phase 2: Tool System
- Implement all built-in tools as KernelFunctions
- Safety tier enforcement with confirmation UX
- MCP server discovery + dynamic tool registration
- Skills/plugins/hooks loading via Extensions packages

### Phase 3: Agent Loop
- SK ChatCompletion with streaming
- Auto-function-calling pipeline
- Compaction integration
- Memory extraction after each turn

### Phase 4: TUI Rendering
- Terminal.Gui layout (chat panel, input bar, status bar)
- Spectre.Console rich rendering (markdown, code, panels)
- Slash command routing + tab completion
- Input history

### Phase 5: Polish & Integration
- Error handling + retry UX
- Export/import chat history
- Custom command loading
- End-to-end testing with all three providers
