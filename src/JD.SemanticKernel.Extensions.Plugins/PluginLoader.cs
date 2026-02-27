using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using JD.SemanticKernel.Extensions.Hooks;
using JD.SemanticKernel.Extensions.Skills;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Plugins;

/// <summary>
/// Loads Claude Code plugin directories into Semantic Kernel components.
/// </summary>
public sealed class PluginLoader
{
    /// <summary>
    /// The standard plugin manifest directory name.
    /// </summary>
    public const string ManifestDirectory = ".claude-plugin";

    /// <summary>
    /// The standard plugin manifest file name.
    /// </summary>
    public const string ManifestFileName = "plugin.json";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Loads a plugin from a directory containing a <c>.claude-plugin/plugin.json</c> manifest.
    /// </summary>
    /// <param name="pluginDirectory">Root directory of the plugin.</param>
    /// <returns>A loaded plugin result containing skills, hooks, and manifest.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the plugin directory doesn't exist.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the manifest file doesn't exist.</exception>
    public static LoadedPlugin Load(string pluginDirectory)
    {
        if (!Directory.Exists(pluginDirectory))
            throw new DirectoryNotFoundException($"Plugin directory not found: {pluginDirectory}");

        var manifestDir = Path.Combine(pluginDirectory, ManifestDirectory);
        var manifestFile = Path.Combine(manifestDir, ManifestFileName);

        PluginManifest manifest;
        if (File.Exists(manifestFile))
        {
            var json = File.ReadAllText(manifestFile);
            manifest = JsonSerializer.Deserialize<PluginManifest>(json, s_jsonOptions)
                ?? new PluginManifest { Name = Path.GetFileName(pluginDirectory) };
        }
        else
        {
            // Allow loading without manifest — use directory name as plugin name
            manifest = new PluginManifest { Name = Path.GetFileName(pluginDirectory) };
        }

        // Load skills
        var skillsDir = Path.Combine(pluginDirectory, manifest.SkillsDir);
        var skills = Directory.Exists(skillsDir)
            ? SkillLoader.LoadFromDirectory(skillsDir)
            : Array.Empty<SkillDefinition>();

        // Load hooks
        var hooksFile = Path.Combine(pluginDirectory, manifest.HooksFile);
        var hooks = File.Exists(hooksFile)
            ? HookParser.ParseFile(hooksFile)
            : Array.Empty<HookDefinition>();

        return new LoadedPlugin(manifest, skills, hooks);
    }

    /// <summary>
    /// Scans a directory for multiple plugin subdirectories.
    /// </summary>
    /// <param name="parentDirectory">Directory containing plugin subdirectories.</param>
    /// <returns>A collection of loaded plugins.</returns>
    public static IReadOnlyList<LoadedPlugin> LoadAll(string parentDirectory)
    {
        if (!Directory.Exists(parentDirectory))
            throw new DirectoryNotFoundException($"Plugins directory not found: {parentDirectory}");

        return Directory.GetDirectories(parentDirectory)
            .Where(d => Directory.Exists(Path.Combine(d, ManifestDirectory))
                        || Directory.Exists(Path.Combine(d, "skills")))
            .Select(Load)
            .ToList()
            .AsReadOnly();
    }
}

/// <summary>
/// Represents a loaded Claude Code plugin with its skills and hooks.
/// </summary>
public sealed class LoadedPlugin
{
    /// <summary>Gets the plugin manifest.</summary>
    public PluginManifest Manifest { get; }

    /// <summary>Gets the loaded skill definitions.</summary>
    public IReadOnlyList<SkillDefinition> Skills { get; }

    /// <summary>Gets the loaded hook definitions.</summary>
    public IReadOnlyList<HookDefinition> Hooks { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="LoadedPlugin"/>.
    /// </summary>
    public LoadedPlugin(
        PluginManifest manifest,
        IReadOnlyList<SkillDefinition> skills,
        IReadOnlyList<HookDefinition> hooks)
    {
        Manifest = manifest;
        Skills = skills;
        Hooks = hooks;
    }

    /// <summary>
    /// Creates a <see cref="KernelPlugin"/> from this loaded plugin's skills.
    /// </summary>
    /// <returns>A kernel plugin containing all skill functions.</returns>
    public KernelPlugin ToKernelPlugin() =>
        SkillKernelFunction.CreatePlugin(Manifest.Name, Skills);
}
