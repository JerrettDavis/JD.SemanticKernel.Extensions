using System;
using JD.SemanticKernel.Extensions.Hooks;
using JD.SemanticKernel.Extensions.Plugins;
using JD.SemanticKernel.Extensions.Skills;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions;

/// <summary>
/// Unified extension methods for loading Claude Code skills, plugins, and hooks into Semantic Kernel.
/// This is a convenience wrapper that re-exports the individual package extension methods.
/// </summary>
public static class KernelBuilderExtensions
{
    /// <summary>
    /// Loads Claude Code SKILL.md files from a directory.
    /// Delegates to <see cref="Skills.KernelBuilderExtensions.UseSkills"/>.
    /// </summary>
    public static IKernelBuilder AddClaudeCodeSkills(
        this IKernelBuilder builder,
        string directoryPath,
        Action<SkillLoadOptions>? configure = null) =>
        Skills.KernelBuilderExtensions.UseSkills(builder, directoryPath, configure);

    /// <summary>
    /// Configures Claude Code lifecycle hooks.
    /// Delegates to <see cref="Hooks.KernelBuilderExtensions.UseHooks"/>.
    /// </summary>
    public static IKernelBuilder AddClaudeCodeHooks(
        this IKernelBuilder builder,
        Action<HookBuilder> configure) =>
        Hooks.KernelBuilderExtensions.UseHooks(builder, configure);

    /// <summary>
    /// Loads a Claude Code plugin directory.
    /// Delegates to <see cref="Plugins.KernelBuilderExtensions.UsePlugins"/>.
    /// </summary>
    public static IKernelBuilder AddClaudeCodePlugin(
        this IKernelBuilder builder,
        string pluginDirectory,
        Action<PluginLoadOptions>? configure = null) =>
        Plugins.KernelBuilderExtensions.UsePlugins(builder, pluginDirectory, configure);

    /// <summary>
    /// Loads all Claude Code plugins from subdirectories.
    /// Delegates to <see cref="Plugins.KernelBuilderExtensions.UseAllPlugins"/>.
    /// </summary>
    public static IKernelBuilder AddClaudeCodePlugins(
        this IKernelBuilder builder,
        string pluginsDirectory,
        Action<PluginLoadOptions>? configure = null) =>
        Plugins.KernelBuilderExtensions.UseAllPlugins(builder, pluginsDirectory, configure);
}
