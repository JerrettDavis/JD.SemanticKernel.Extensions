using System;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Skills;

/// <summary>
/// Extension methods for <see cref="IKernelBuilder"/> to load Claude Code skills.
/// </summary>
public static class KernelBuilderExtensions
{
    /// <summary>
    /// Loads Claude Code SKILL.md files from a directory and registers them as kernel functions.
    /// </summary>
    /// <param name="builder">The kernel builder.</param>
    /// <param name="directoryPath">Path to the directory containing SKILL.md files.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The kernel builder for chaining.</returns>
    public static IKernelBuilder UseSkills(
        this IKernelBuilder builder,
        string directoryPath,
        Action<SkillLoadOptions>? configure = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(directoryPath);
#else
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (directoryPath is null) throw new ArgumentNullException(nameof(directoryPath));
#endif

        var options = new SkillLoadOptions();
        configure?.Invoke(options);

        var skills = SkillLoader.LoadFromDirectory(directoryPath, options.Recursive);

        if (skills.Count == 0)
            return builder;

        var plugin = SkillKernelFunction.CreatePlugin(options.PluginName, skills);
        builder.Plugins.Add(plugin);

        return builder;
    }

    /// <summary>
    /// Loads a single Claude Code SKILL.md file and registers it as a kernel function.
    /// </summary>
    /// <param name="builder">The kernel builder.</param>
    /// <param name="filePath">Path to the SKILL.md file.</param>
    /// <param name="pluginName">Optional plugin name. Defaults to "Skills".</param>
    /// <returns>The kernel builder for chaining.</returns>
    public static IKernelBuilder UseSkillFile(
        this IKernelBuilder builder,
        string filePath,
        string pluginName = "Skills")
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(filePath);
#else
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (filePath is null) throw new ArgumentNullException(nameof(filePath));
#endif

        var definition = SkillLoader.LoadFromFile(filePath);
        var function = SkillKernelFunction.Create(definition);
        var plugin = KernelPluginFactory.CreateFromFunctions(pluginName, [function]);
        builder.Plugins.Add(plugin);

        return builder;
    }
}
