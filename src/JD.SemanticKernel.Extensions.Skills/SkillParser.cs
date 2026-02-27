using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace JD.SemanticKernel.Extensions.Skills;

/// <summary>
/// Parses Claude Code SKILL.md files into <see cref="SkillDefinition"/> instances.
/// </summary>
public static class SkillParser
{
    private static readonly Regex s_frontmatterRegex = new(
        @"^---\s*\r?\n(?<yaml>.*?)\r?\n---\s*\r?\n",
        RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(5));

    private static readonly Regex s_argumentRegex = new(
        @"\$(?:ARGUMENTS|(?<num>\d+))\b",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(5));

    private static readonly IDeserializer s_yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Parses a SKILL.md file from its text content.
    /// </summary>
    /// <param name="content">The full text content of the SKILL.md file.</param>
    /// <param name="sourcePath">Optional source file path for diagnostics.</param>
    /// <returns>A <see cref="SkillDefinition"/> representing the parsed skill.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    public static SkillDefinition Parse(string content, string? sourcePath = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(content);
#else
        if (content is null) throw new ArgumentNullException(nameof(content));
#endif

        var definition = new SkillDefinition { SourcePath = sourcePath };
        var match = s_frontmatterRegex.Match(content);

        if (match.Success)
        {
            var yaml = match.Groups["yaml"].Value;
            ParseFrontmatter(yaml, definition);
            definition.Body = content.Substring(match.Index + match.Length).Trim();
        }
        else
        {
            definition.Body = content.Trim();
        }

        // Extract argument references from body
        ExtractArguments(definition);

        // Derive name from body heading if not set in frontmatter
        if (string.IsNullOrEmpty(definition.Name))
            definition.Name = DeriveNameFromBody(definition.Body) ?? "unnamed-skill";

        return definition;
    }

    /// <summary>
    /// Parses a SKILL.md file from a file path.
    /// </summary>
    /// <param name="filePath">Path to the SKILL.md file.</param>
    /// <returns>A <see cref="SkillDefinition"/> representing the parsed skill.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static SkillDefinition ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("SKILL.md file not found.", filePath);

        var content = File.ReadAllText(filePath);
        return Parse(content, filePath);
    }

    private static void ParseFrontmatter(string yaml, SkillDefinition definition)
    {
        var data = s_yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
        if (data is null)
            return;

        if (data.TryGetValue("name", out var name))
            definition.Name = name?.ToString() ?? string.Empty;

        if (data.TryGetValue("description", out var desc))
            definition.Description = desc?.ToString() ?? string.Empty;

        if (data.TryGetValue("allowed-tools", out var tools))
        {
            if (tools is List<object> toolList)
            {
                foreach (var tool in toolList)
                {
                    var toolStr = tool?.ToString();
                    if (toolStr is not null)
                        definition.AllowedTools.Add(toolStr);
                }
            }
            else if (tools is string toolStr)
            {
                foreach (var tool in toolStr.Split(','))
                {
                    var trimmed = tool.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        definition.AllowedTools.Add(trimmed);
                }
            }
        }

        // Store remaining metadata
        var knownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "name", "description", "allowed-tools"
        };

        foreach (var kvp in data)
        {
            if (!knownKeys.Contains(kvp.Key))
                definition.Metadata[kvp.Key] = kvp.Value;
        }
    }

    private static void ExtractArguments(SkillDefinition definition)
    {
        var matches = s_argumentRegex.Matches(definition.Body);
        foreach (Match m in matches)
        {
            if (m.Groups["num"].Success)
                definition.Arguments[m.Groups["num"].Value] = $"Argument ${m.Groups["num"].Value}";
            else
                definition.Arguments["ARGUMENTS"] = "The input arguments for the skill";
        }
    }

    private static string? DeriveNameFromBody(string body)
    {
        using var reader = new StringReader(body);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
#if NET8_0_OR_GREATER
            if (line.StartsWith('#'))
#else
            if (line.StartsWith("#", StringComparison.Ordinal))
#endif
            {
                var heading = line.TrimStart('#').Trim();
                if (!string.IsNullOrEmpty(heading))
                    return Slugify(heading);
            }
        }

        return null;
    }

#pragma warning disable CA1308 // Slug generation requires lowercase
    private static string Slugify(string text)
    {
        var slug = text.ToLowerInvariant()
            .Replace(' ', '-');
        return Regex.Replace(slug, @"[^a-z0-9\-]", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(5));
    }
#pragma warning restore CA1308
}
