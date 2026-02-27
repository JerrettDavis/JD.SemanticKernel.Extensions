using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Skills;

/// <summary>
/// Adapts a <see cref="SkillDefinition"/> into a Semantic Kernel <see cref="KernelFunction"/>.
/// </summary>
public static class SkillKernelFunction
{
    /// <summary>
    /// Creates a <see cref="KernelFunction"/> from a <see cref="SkillDefinition"/>.
    /// The skill's markdown body becomes the prompt template, and arguments
    /// map to <see cref="KernelParameterMetadata"/>.
    /// </summary>
    /// <param name="definition">The skill definition to convert.</param>
    /// <returns>A <see cref="KernelFunction"/> that can be invoked via the SK kernel.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="definition"/> is null.</exception>
    public static KernelFunction Create(SkillDefinition definition)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(definition);
#else
        if (definition is null) throw new ArgumentNullException(nameof(definition));
#endif

        // Build the prompt template from the skill body
        // Replace $ARGUMENTS with {{$input}} for SK template syntax
        var template = definition.Body.Replace("$ARGUMENTS", "{{$input}}");

        // Replace positional args $0, $1, etc.
        foreach (var arg in definition.Arguments)
        {
            if (!string.Equals(arg.Key, "ARGUMENTS", StringComparison.Ordinal))
                template = template.Replace($"${arg.Key}", $"{{{{$arg{arg.Key}}}}}");
        }

        var promptConfig = new PromptTemplateConfig(template)
        {
            Name = SanitizeFunctionName(definition.Name),
            Description = !string.IsNullOrEmpty(definition.Description)
                ? definition.Description
                : $"Skill loaded from {definition.SourcePath ?? "unknown"}"
        };

        // Add input parameters
        if (definition.Arguments.ContainsKey("ARGUMENTS"))
        {
            promptConfig.InputVariables.Add(new InputVariable
            {
                Name = "input",
                Description = "The input arguments for the skill",
                IsRequired = false
            });
        }

        foreach (var arg in definition.Arguments.Where(a => !string.Equals(a.Key, "ARGUMENTS", StringComparison.Ordinal)))
        {
            promptConfig.InputVariables.Add(new InputVariable
            {
                Name = $"arg{arg.Key}",
                Description = arg.Value,
                IsRequired = false
            });
        }

        return KernelFunctionFactory.CreateFromPrompt(promptConfig);
    }

    /// <summary>
    /// Creates a <see cref="KernelPlugin"/> containing multiple skill functions.
    /// </summary>
    /// <param name="pluginName">Name for the plugin grouping.</param>
    /// <param name="definitions">The skill definitions to include.</param>
    /// <returns>A <see cref="KernelPlugin"/> containing all skill functions.</returns>
    public static KernelPlugin CreatePlugin(
        string pluginName,
        IEnumerable<SkillDefinition> definitions)
    {
        var sanitized = SanitizeFunctionName(pluginName);
        var functions = definitions.Select(Create).ToList();
        return KernelPluginFactory.CreateFromFunctions(sanitized, functions);
    }

    private static string SanitizeFunctionName(string name) =>
        new string(name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray());
}
