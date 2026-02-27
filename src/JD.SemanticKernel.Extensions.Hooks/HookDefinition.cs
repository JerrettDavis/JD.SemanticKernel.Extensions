using System;
using System.Collections.Generic;

namespace JD.SemanticKernel.Extensions.Hooks;

/// <summary>
/// Represents a parsed Claude Code hook definition.
/// </summary>
public sealed class HookDefinition
{
    /// <summary>
    /// Gets or sets the hook event type.
    /// </summary>
    public HookEvent Event { get; set; }

    /// <summary>
    /// Gets or sets the tool name pattern (regex) this hook matches against.
    /// For example, "Bash|Execute" matches either tool.
    /// </summary>
    public string? ToolPattern { get; set; }

    /// <summary>
    /// Gets or sets the hook type.
    /// </summary>
    public HookType Type { get; set; }

    /// <summary>
    /// Gets or sets the command to execute (for <see cref="HookType.Command"/> hooks).
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// Gets or sets the prompt template (for <see cref="HookType.Prompt"/> hooks).
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// Gets or sets the timeout in milliseconds. Defaults to 30000 (30 seconds).
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets additional metadata properties.
    /// </summary>
    public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>(StringComparer.Ordinal);
}

/// <summary>
/// Claude Code hook lifecycle events mapped to Semantic Kernel filter points.
/// </summary>
public enum HookEvent
{
    /// <summary>Before a tool/function is invoked.</summary>
    PreToolUse,

    /// <summary>After a tool/function is invoked.</summary>
    PostToolUse,

    /// <summary>Before a user prompt is submitted.</summary>
    UserPromptSubmit,

    /// <summary>Before context compaction occurs.</summary>
    PreCompact,

    /// <summary>When a session starts.</summary>
    SessionStart,

    /// <summary>When a session ends.</summary>
    SessionEnd,

    /// <summary>When the agent stops.</summary>
    Stop,

    /// <summary>When a sub-agent stops.</summary>
    SubagentStop,

    /// <summary>Informational notification.</summary>
    Notification
}

/// <summary>
/// The type of hook handler.
/// </summary>
public enum HookType
{
    /// <summary>Execute a shell command.</summary>
    Command,

    /// <summary>Run a prompt through the LLM for validation.</summary>
    Prompt,

    /// <summary>A managed delegate/callback handler.</summary>
    Delegate
}
