using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JD.SemanticKernel.Extensions.Plugins;

/// <summary>
/// Represents a parsed Claude Code plugin.json manifest.
/// </summary>
public sealed class PluginManifest
{
    /// <summary>Gets or sets the plugin name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the plugin version.</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.0.0";

    /// <summary>Gets or sets the plugin description.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the list of plugin dependencies (other plugin names).</summary>
    [JsonPropertyName("dependencies")]
#pragma warning disable CA2227 // Collection properties should be read only — needed for JSON deserialization
    public IList<string> Dependencies { get; set; } = new List<string>();
#pragma warning restore CA2227

    /// <summary>Gets or sets the skills directory relative path.</summary>
    [JsonPropertyName("skills_dir")]
    public string SkillsDir { get; set; } = "skills";

    /// <summary>Gets or sets the hooks file relative path.</summary>
    [JsonPropertyName("hooks_file")]
    public string HooksFile { get; set; } = "hooks/hooks.json";

    /// <summary>Gets or sets the MCP configuration file relative path.</summary>
    [JsonPropertyName("mcp_config")]
    public string? McpConfig { get; set; }

    /// <summary>Gets or sets additional metadata.</summary>
    [JsonExtensionData]
#pragma warning disable CA2227 // Collection properties should be read only — needed for JSON extension data
    public IDictionary<string, object>? ExtensionData { get; set; }
#pragma warning restore CA2227
}
