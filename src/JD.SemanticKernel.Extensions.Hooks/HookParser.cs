using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JD.SemanticKernel.Extensions.Hooks;

/// <summary>
/// Parses Claude Code hooks.json files into <see cref="HookDefinition"/> instances.
/// </summary>
public static class HookParser
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Parses hooks from a JSON string.
    /// </summary>
    /// <param name="json">JSON content containing hook definitions.</param>
    /// <returns>A list of parsed hook definitions.</returns>
    public static IReadOnlyList<HookDefinition> Parse(string json)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(json);
#else
        if (json is null) throw new ArgumentNullException(nameof(json));
#endif

        var container = JsonSerializer.Deserialize<HookContainer>(json, s_jsonOptions);
        if (container?.Hooks is null)
            return Array.Empty<HookDefinition>();

        var definitions = new List<HookDefinition>();
        foreach (var hook in container.Hooks)
        {
            var definition = new HookDefinition
            {
                Event = ParseEvent(hook.Event),
                ToolPattern = hook.ToolName,
                TimeoutMs = hook.TimeoutMs ?? 30000
            };

            if (!string.IsNullOrEmpty(hook.Command))
            {
                definition.Type = HookType.Command;
                definition.Command = hook.Command;
            }
            else if (!string.IsNullOrEmpty(hook.Prompt))
            {
                definition.Type = HookType.Prompt;
                definition.Prompt = hook.Prompt;
            }

            definitions.Add(definition);
        }

        return definitions.AsReadOnly();
    }

    /// <summary>
    /// Parses hooks from a file path.
    /// </summary>
    /// <param name="filePath">Path to the hooks.json file.</param>
    /// <returns>A list of parsed hook definitions.</returns>
    public static IReadOnlyList<HookDefinition> ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Hooks file not found.", filePath);

        var json = File.ReadAllText(filePath);
        return Parse(json);
    }

    private static HookEvent ParseEvent(string? eventName) =>
        eventName?.ToLowerInvariant() switch
        {
            "pretooluse" => HookEvent.PreToolUse,
            "posttooluse" => HookEvent.PostToolUse,
            "userpromptsubmit" => HookEvent.UserPromptSubmit,
            "precompact" => HookEvent.PreCompact,
            "sessionstart" => HookEvent.SessionStart,
            "sessionend" => HookEvent.SessionEnd,
            "stop" => HookEvent.Stop,
            "subagentstop" => HookEvent.SubagentStop,
            "notification" => HookEvent.Notification,
            _ => HookEvent.Notification
        };

    private sealed class HookContainer
    {
        public List<HookEntry>? Hooks { get; set; }
    }

    private sealed class HookEntry
    {
        public string? Event { get; set; }
        [JsonPropertyName("tool_name")]
        public string? ToolName { get; set; }
        public string? Command { get; set; }
        public string? Prompt { get; set; }
        [JsonPropertyName("timeout_ms")]
        public int? TimeoutMs { get; set; }
    }
}
