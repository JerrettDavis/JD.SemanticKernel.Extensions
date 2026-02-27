using System;
using JD.SemanticKernel.Extensions.Skills;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Plugins;

/// <summary>
/// Options for configuring how plugins are loaded.
/// </summary>
public sealed class PluginLoadOptions
{
    /// <summary>
    /// Gets or sets how skills within the plugin are loaded.
    /// Defaults to <see cref="SkillLoadMode.AsKernelFunction"/>.
    /// </summary>
    public SkillLoadMode SkillMode { get; set; } = SkillLoadMode.AsKernelFunction;

    /// <summary>
    /// Gets or sets whether hooks defined in the plugin should be registered.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool EnableHooks { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to resolve dependencies across loaded plugins.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool ResolveDependencies { get; set; } = true;
}

/// <summary>
/// Extension methods for <see cref="IKernelBuilder"/> to load Claude Code plugins.
/// </summary>
public static class KernelBuilderExtensions
{
    /// <summary>
    /// Loads a Claude Code plugin from a directory and registers its skills and hooks.
    /// </summary>
    /// <param name="builder">The kernel builder.</param>
    /// <param name="pluginDirectory">Root directory of the plugin.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The kernel builder for chaining.</returns>
    public static IKernelBuilder UsePlugins(
        this IKernelBuilder builder,
        string pluginDirectory,
        Action<PluginLoadOptions>? configure = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(pluginDirectory);
#else
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (pluginDirectory is null) throw new ArgumentNullException(nameof(pluginDirectory));
#endif

        var options = new PluginLoadOptions();
        configure?.Invoke(options);

        var loaded = PluginLoader.Load(pluginDirectory);
        RegisterPlugin(builder, loaded);

        return builder;
    }

    /// <summary>
    /// Loads all Claude Code plugins from subdirectories and registers their skills and hooks.
    /// </summary>
    /// <param name="builder">The kernel builder.</param>
    /// <param name="pluginsDirectory">Parent directory containing plugin subdirectories.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The kernel builder for chaining.</returns>
    public static IKernelBuilder UseAllPlugins(
        this IKernelBuilder builder,
        string pluginsDirectory,
        Action<PluginLoadOptions>? configure = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(pluginsDirectory);
#else
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (pluginsDirectory is null) throw new ArgumentNullException(nameof(pluginsDirectory));
#endif

        var options = new PluginLoadOptions();
        configure?.Invoke(options);

        var plugins = PluginLoader.LoadAll(pluginsDirectory);

        var ordered = options.ResolveDependencies
            ? PluginDependencyResolver.Resolve(plugins)
            : plugins;

        foreach (var plugin in ordered)
            RegisterPlugin(builder, plugin);

        return builder;
    }

    private static void RegisterPlugin(IKernelBuilder builder, LoadedPlugin loaded)
    {
        if (loaded.Skills.Count > 0)
        {
            var kernelPlugin = loaded.ToKernelPlugin();
            builder.Plugins.Add(kernelPlugin);
        }
    }
}
