using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JD.SemanticKernel.Extensions.Skills;

/// <summary>
/// Loads Claude Code SKILL.md files from directories.
/// </summary>
public static class SkillLoader
{
    /// <summary>
    /// The standard skill file name.
    /// </summary>
    public const string SkillFileName = "SKILL.md";

    /// <summary>
    /// Loads all SKILL.md files from a directory.
    /// </summary>
    /// <param name="directoryPath">Root directory to scan.</param>
    /// <param name="recursive">Whether to scan subdirectories recursively.</param>
    /// <returns>A collection of parsed <see cref="SkillDefinition"/> instances.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    public static IReadOnlyList<SkillDefinition> LoadFromDirectory(
        string directoryPath,
        bool recursive = true)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Skills directory not found: {directoryPath}");

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(directoryPath, SkillFileName, searchOption);

        return files
            .Select(SkillParser.ParseFile)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Loads a single SKILL.md file.
    /// </summary>
    /// <param name="filePath">Path to the SKILL.md file.</param>
    /// <returns>A parsed <see cref="SkillDefinition"/>.</returns>
    public static SkillDefinition LoadFromFile(string filePath) =>
        SkillParser.ParseFile(filePath);
}
