# Conversation Persistence & Session Recall — Design Document

## Problem Statement

jdai currently has no memory between sessions. Every launch starts fresh — there's no way to resume a conversation, review what was discussed, see which tools ran, or roll back to an earlier point. Claude Code, Copilot, and similar tools track all of this. We need full conversational recall with persistence, resumption, and timeline navigation.

## Goals

1. **Persist every session** — all prompts, responses, thinking, tool calls, files touched, tokens, models
2. **Resume conversations** — `/resume` to pick up where you left off
3. **Name sessions** — `/name refactor-auth` for easy recall
4. **Full timeline** — double-ESC when idle opens an interactive history browser
5. **Rollback** — restore conversation state to any prior turn
6. **Hybrid storage** — SQLite for fast queries, JSON for human-readable export/backup
7. **Integrity** — cross-check between stores, auto-repair from either direction

## Storage Architecture

### Directory Layout

```
~/.jdai/
  sessions.db                          ← SQLite: index of ALL sessions
  projects/
    <sha256-of-cwd-first-8-chars>/
      sessions/
        <session-uuid>.json            ← Full human-readable snapshot
```

### SQLite Schema (`sessions.db`)

```sql
CREATE TABLE sessions (
    id              TEXT PRIMARY KEY,       -- UUID
    name            TEXT,                   -- user-assigned or auto-generated
    project_path    TEXT NOT NULL,          -- absolute working directory
    project_hash    TEXT NOT NULL,          -- SHA256(project_path)[0:8]
    model_id        TEXT,                   -- initial model
    provider_name   TEXT,                   -- initial provider
    created_at      TEXT NOT NULL,          -- ISO 8601
    updated_at      TEXT NOT NULL,          -- ISO 8601
    total_tokens    INTEGER DEFAULT 0,
    message_count   INTEGER DEFAULT 0,
    is_active       INTEGER DEFAULT 1       -- 1 = current, 0 = ended
);

CREATE TABLE turns (
    id              TEXT PRIMARY KEY,       -- UUID
    session_id      TEXT NOT NULL REFERENCES sessions(id),
    turn_index      INTEGER NOT NULL,       -- 0-based sequential
    role            TEXT NOT NULL,           -- user | assistant | system
    content         TEXT,                   -- message text
    thinking_text   TEXT,                   -- reasoning/thinking (ephemeral display)
    model_id        TEXT,                   -- model used for this turn
    provider_name   TEXT,
    tokens_in       INTEGER DEFAULT 0,
    tokens_out      INTEGER DEFAULT 0,
    duration_ms     INTEGER DEFAULT 0,
    created_at      TEXT NOT NULL
);

CREATE TABLE tool_calls (
    id              TEXT PRIMARY KEY,
    turn_id         TEXT NOT NULL REFERENCES turns(id),
    tool_name       TEXT NOT NULL,
    arguments       TEXT,                   -- JSON of args
    result          TEXT,                   -- truncated result
    status          TEXT DEFAULT 'ok',      -- ok | error | denied
    duration_ms     INTEGER DEFAULT 0,
    created_at      TEXT NOT NULL
);

CREATE TABLE files_touched (
    id              TEXT PRIMARY KEY,
    turn_id         TEXT NOT NULL REFERENCES turns(id),
    file_path       TEXT NOT NULL,
    operation       TEXT NOT NULL,           -- read | write | edit | delete | exec
    created_at      TEXT NOT NULL
);

CREATE INDEX idx_sessions_project ON sessions(project_hash);
CREATE INDEX idx_sessions_updated ON sessions(updated_at DESC);
CREATE INDEX idx_turns_session ON turns(session_id, turn_index);
CREATE INDEX idx_tool_calls_turn ON tool_calls(turn_id);
CREATE INDEX idx_files_touched_turn ON files_touched(turn_id);
```

### JSON Export Format (`<session-uuid>.json`)

```json
{
  "id": "a1b2c3d4-...",
  "name": "refactor-auth-module",
  "projectPath": "/home/user/myproject",
  "model": "qwen3-coder:30b",
  "provider": "Ollama",
  "createdAt": "2026-02-27T19:00:00Z",
  "updatedAt": "2026-02-27T19:55:00Z",
  "totalTokens": 12345,
  "turns": [
    {
      "index": 0,
      "role": "user",
      "content": "Refactor the auth module to use JWT",
      "createdAt": "2026-02-27T19:00:01Z"
    },
    {
      "index": 1,
      "role": "assistant",
      "content": "I'll refactor the auth module...",
      "thinkingText": "Let me analyze the current auth structure...",
      "model": "qwen3-coder:30b",
      "tokensIn": 150,
      "tokensOut": 800,
      "durationMs": 5200,
      "toolCalls": [
        {
          "toolName": "read_file",
          "arguments": {"path": "src/auth.ts"},
          "result": "...",
          "status": "ok",
          "durationMs": 12
        }
      ],
      "filesTouched": [
        {"path": "src/auth.ts", "operation": "read"},
        {"path": "src/auth.ts", "operation": "edit"}
      ],
      "createdAt": "2026-02-27T19:00:02Z"
    }
  ]
}
```

## Implementation Steps

### Step 1: Data Models (`Persistence/SessionRecord.cs`)

Create POCO records for all entities:

```csharp
public sealed record SessionInfo
{
    public string Id { get; init; }              // UUID
    public string? Name { get; set; }            // mutable — user can /name
    public string ProjectPath { get; init; }
    public string ProjectHash { get; init; }
    public string? ModelId { get; init; }
    public string? ProviderName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
    public long TotalTokens { get; set; }
    public int MessageCount { get; set; }
    public bool IsActive { get; set; }
    public List<TurnRecord> Turns { get; init; } = [];
}

public sealed record TurnRecord { ... }
public sealed record ToolCallRecord { ... }
public sealed record FileTouchRecord { ... }
```

**Validation:** Records compile, can be instantiated, JSON-serialized.

### Step 2: SQLite Store (`Persistence/SessionStore.cs`)

Implements all CRUD operations against `sessions.db`:

- `InitializeAsync()` — creates DB and tables if not exist
- `CreateSessionAsync(SessionInfo)` — inserts session row
- `SaveTurnAsync(TurnRecord, List<ToolCallRecord>, List<FileTouchRecord>)` — inserts turn + related rows
- `UpdateSessionAsync(SessionInfo)` — updates tokens, name, updated_at
- `GetSessionAsync(string id)` — loads full session with all turns
- `ListSessionsAsync(string? projectHash, int limit)` — recent sessions
- `DeleteTurnsAfterAsync(string sessionId, int turnIndex)` — for rollback
- `CloseSessionAsync(string id)` — sets is_active = 0

Uses `Microsoft.Data.Sqlite` directly (no EF). Connection pooling via a single connection string.

**Validation:** Unit tests with in-memory SQLite (`:memory:`).

### Step 3: JSON Exporter (`Persistence/SessionExporter.cs`)

- `ExportAsync(SessionInfo, string outputDir)` — writes the full JSON snapshot
- `ImportAsync(string jsonPath)` — reads a JSON file back into a `SessionInfo`
- `ExportDir(string projectHash)` — returns `~/.jdai/projects/<hash>/sessions/`

Uses `System.Text.Json` with indented formatting and camelCase naming.

**Validation:** Round-trip test — export then import, assert equality.

### Step 4: Integrity Checker (`Persistence/SessionIntegrity.cs`)

- `CheckAndRepairAsync(SessionStore, string projectHash)` — cross-checks SQLite vs JSON
  - If SQLite has a session but JSON is missing → re-export
  - If JSON exists but SQLite is missing → re-import
  - If both exist, compare message_count; reconcile from the more complete one

Runs on startup (fast — just checks counts, not full content).

**Validation:** Test with deliberately missing/corrupted data in each store.

### Step 5: Wire into AgentSession (`Agent/AgentSession.cs`)

Add tracking state to `AgentSession`:

- `SessionId` — UUID assigned on creation
- `CurrentTurnIndex` — increments each turn
- `CurrentTurnToolCalls` — list accumulating tool calls for the active turn
- `CurrentTurnFilesTouched` — list accumulating file operations
- `CurrentTurnThinking` — accumulated thinking text

`AgentLoop.RunTurnStreamingAsync` calls `AgentSession.BeginTurn()` at start and `AgentSession.EndTurn()` at end. `EndTurn()` triggers `SessionStore.SaveTurnAsync()` + `SessionExporter.ExportAsync()`.

`ToolConfirmationFilter` calls `AgentSession.RecordToolCall()` after each invocation.

**Validation:** Existing unit tests still pass + new tests for tracking.

### Step 6: New Slash Commands

Add to `SlashCommandRouter`:

| Command | Handler | Behavior |
|---------|---------|----------|
| `/resume` | `ResumeAsync()` | List recent sessions for this project, prompt selection |
| `/resume --all` | `ResumeAllAsync()` | List sessions across all projects |
| `/name <name>` | `NameSession()` | Set session name |
| `/history` | `ShowHistory()` | Print turn timeline |
| `/rollback <n>` | `RollbackAsync()` | Restore to turn N, delete after |
| `/export` | `ExportAsync()` | Force JSON export |
| `/sessions` | alias for `/resume` | |

Register all in `CompletionProvider` for autocomplete.

**Validation:** Unit tests for each command.

### Step 7: History Viewer (`Rendering/HistoryViewer.cs`)

Interactive turn browser triggered by double-ESC when idle:

```
╭─ Session: refactor-auth-module ─────────────────────╮
│  #0  [user]    Refactor the auth module to use JWT   │
│  #1  [assist]  I'll refactor... (3 tools, 2 files)   │
│ ▸#2  [user]    Also add refresh token support         │
│  #3  [assist]  Done. Added refresh... (5 tools)       │
╰─────────────────────────────── ESC to close, R to rollback ─╯
```

Uses Spectre.Console `Table` or raw ANSI for the interactive list. Arrow keys navigate, Enter shows details, R rolls back, ESC dismisses.

**Validation:** Manual testing (interactive UI).

### Step 8: CLI Flags & Startup Flow (`Program.cs`)

New CLI args:
- `--resume` — auto-resume last active session
- `--resume <id-or-name>` — resume specific session
- `--new` — force new session (skip resume prompt)

Startup flow:
1. Initialize `SessionStore`
2. Run `SessionIntegrity.CheckAndRepairAsync()`
3. Check for `--resume` flag
4. If no flag, check for active sessions in this project → offer to resume
5. Create or restore `AgentSession` with session ID
6. Enter main loop

### Step 9: Idle ESC Handler

Modify `Program.cs` main loop:
- At the prompt (before `ReadInput`), check for ESC keypress
- If double-ESC detected at the prompt → open `HistoryViewer`
- This is separate from `TurnInputMonitor` (which handles ESC during processing)

### Step 10: NuGet Dependency

Add `Microsoft.Data.Sqlite` to `JD.AI.Tui.csproj`:

```xml
<PackageReference Include="Microsoft.Data.Sqlite" />
```

Add version to `Directory.Packages.props`.

## Testing Strategy

- **SessionStore:** In-memory SQLite tests — CRUD, rollback, list, close
- **SessionExporter:** Round-trip JSON tests — export/import/compare
- **SessionIntegrity:** Missing/corrupted data recovery tests
- **SessionRecord:** Serialization/deserialization tests
- **StreamingContentParser:** Already covered (19 tests)
- **SlashCommandRouter:** New command tests (resume, name, history, rollback, export)
- **HistoryViewer:** Manual testing only (interactive UI)
- **Integration:** Full session lifecycle — create, turns, resume, rollback, export

## Execution Order

1. Step 10 (NuGet dep)
2. Step 1 (Data models)
3. Step 2 (SQLite store + tests)
4. Step 3 (JSON exporter + tests)
5. Step 4 (Integrity checker + tests)
6. Step 5 (Wire into AgentSession)
7. Step 6 (Slash commands + tests)
8. Step 8 (CLI flags + startup)
9. Step 9 (Idle ESC handler)
10. Step 7 (History viewer)
