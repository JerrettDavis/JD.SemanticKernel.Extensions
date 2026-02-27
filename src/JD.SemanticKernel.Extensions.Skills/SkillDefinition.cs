using System.Collections.Generic;

namespace JD.SemanticKernel.Extensions.Skills;

/// <summary>
/// Represents a parsed Claude Code SKILL.md definition.
/// </summary>
public sealed class SkillDefinition
{
    /// <summary>
    /// Gets or sets the skill name from YAML frontmatter.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the skill description from YAML frontmatter.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the markdown body (instructions) of the skill.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets the list of allowed tools from YAML frontmatter.
    /// </summary>
    public IList<string> AllowedTools { get; } = new List<string>();

    /// <summary>
    /// Gets the argument definitions parsed from the skill body.
    /// Keys are argument names (e.g., "ARGUMENTS", "0", "1").
    /// </summary>
    public IDictionary<string, string> Arguments { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets additional YAML frontmatter properties not explicitly mapped.
    /// </summary>
    public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the source file path of the SKILL.md.
    /// </summary>
    public string? SourcePath { get; set; }
}
