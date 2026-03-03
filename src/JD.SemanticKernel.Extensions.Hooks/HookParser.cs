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

#pragma warning disable CA1308 // Event matching requires lowercase comparison
    private static HookEvent ParseEvent(string? eventName) =>
        eventName?.ToLowerInvariant() switch
#pragma warning restore CA1308
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
        [JsonConverter(typeof(HooksPropertyConverter))]
        public List<HookEntry>? Hooks { get; set; }
    }

    /// <summary>
    /// Handles both hook formats:
    /// - Legacy flat array: <c>{"hooks": [{"event": "PreToolUse", ...}]}</c>
    /// - Claude Code nested dict: <c>{"hooks": {"PreToolUse": [{"hooks": [{"command": "..."}]}]}}</c>
    /// </summary>
    private sealed class HooksPropertyConverter : JsonConverter<List<HookEntry>?>
    {
        public override List<HookEntry>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            // Flat array format
            if (reader.TokenType == JsonTokenType.StartArray)
                return JsonSerializer.Deserialize<List<HookEntry>>(ref reader, options);

            // Dictionary format: {"PreToolUse": [...], "PostToolUse": [...]}
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                var result = new List<HookEntry>();
                using var doc = JsonDocument.ParseValue(ref reader);

                foreach (var eventProp in doc.RootElement.EnumerateObject())
                {
                    var eventName = eventProp.Name;
                    if (eventProp.Value.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var group in eventProp.Value.EnumerateArray())
                    {
                        // Each group may have a nested "hooks" array
                        if (group.TryGetProperty("hooks", out var innerHooks) &&
                            innerHooks.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var hook in innerHooks.EnumerateArray())
                            {
                                result.Add(new HookEntry
                                {
                                    Event = eventName,
                                    Command = hook.TryGetProperty("command", out var cmd) ? cmd.GetString() : null,
                                    Prompt = hook.TryGetProperty("prompt", out var prompt) ? prompt.GetString() : null,
                                    TimeoutMs = hook.TryGetProperty("timeout", out var t) && t.TryGetInt32(out var tv) ? tv * 1000
                                              : hook.TryGetProperty("timeout_ms", out var tms) && tms.TryGetInt32(out var tmsv) ? tmsv
                                              : null,
                                    ToolName = hook.TryGetProperty("tool_name", out var tn) ? tn.GetString() : null,
                                });
                            }
                        }
                        else
                        {
                            // Flat entry within the event group
                            result.Add(new HookEntry
                            {
                                Event = eventName,
                                Command = group.TryGetProperty("command", out var cmd) ? cmd.GetString() : null,
                                Prompt = group.TryGetProperty("prompt", out var prompt) ? prompt.GetString() : null,
                                TimeoutMs = group.TryGetProperty("timeout_ms", out var tms) && tms.TryGetInt32(out var tmsv) ? tmsv : null,
                                ToolName = group.TryGetProperty("tool_name", out var tn) ? tn.GetString() : null,
                            });
                        }
                    }
                }

                return result;
            }

            throw new JsonException($"Unexpected token type '{reader.TokenType}' for hooks property.");
        }

        public override void Write(Utf8JsonWriter writer, List<HookEntry>? value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value, options);
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
