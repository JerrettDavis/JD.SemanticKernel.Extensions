using System;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Skills;

/// <summary>
/// Options for configuring how skills are loaded and registered.
/// </summary>
public sealed class SkillLoadOptions
{
    /// <summary>
    /// Gets or sets the plugin name used when registering skills as a <see cref="KernelPlugin"/>.
    /// Defaults to "Skills".
    /// </summary>
    public string PluginName { get; set; } = "Skills";

    /// <summary>
    /// Gets or sets whether to scan subdirectories recursively. Defaults to <c>true</c>.
    /// </summary>
    public bool Recursive { get; set; } = true;

    /// <summary>
    /// Gets or sets the skill loading mode. Defaults to <see cref="SkillLoadMode.AsKernelFunction"/>.
    /// </summary>
    public SkillLoadMode Mode { get; set; } = SkillLoadMode.AsKernelFunction;
}

/// <summary>
/// Determines how a skill is registered with the Semantic Kernel.
/// </summary>
public enum SkillLoadMode
{
    /// <summary>
    /// Register each skill as a callable <see cref="KernelFunction"/>.
    /// </summary>
    AsKernelFunction,

    /// <summary>
    /// Register each skill as a prompt template for use as system prompts.
    /// </summary>
    AsPromptTemplate
}
