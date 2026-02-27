// Skill Demo — loads SKILL.md files and shows registered kernel functions.
// Full interactive demo requires an AI provider (e.g., OpenAI, Claude Code, Copilot).

using JD.SemanticKernel.Extensions.Skills;
using Microsoft.SemanticKernel;

Console.WriteLine("=== SK Extensions — Skill Demo ===");
Console.WriteLine();

// Create a kernel builder
var builder = Kernel.CreateBuilder();

// Check for skills directory
var skillsDir = Path.Combine(AppContext.BaseDirectory, "skills");
if (!Directory.Exists(skillsDir))
{
    Console.WriteLine($"Creating sample skill at: {skillsDir}");
    Directory.CreateDirectory(Path.Combine(skillsDir, "code-reviewer"));
    File.WriteAllText(
        Path.Combine(skillsDir, "code-reviewer", "SKILL.md"),
        """
        ---
        name: code-reviewer
        description: Reviews code for quality issues, bugs, and security vulnerabilities
        allowed-tools: [Read, Grep, Glob]
        ---
        # Code Reviewer

        Review the provided code for:
        1. Bug risks and logic errors
        2. Security vulnerabilities
        3. Performance issues
        4. Code style and readability

        Input: $ARGUMENTS
        """);
}

// Load skills
builder.UseSkills(skillsDir);

var kernel = builder.Build();

Console.WriteLine($"Loaded {kernel.Plugins.Count} plugin(s):");
foreach (var plugin in kernel.Plugins)
{
    Console.WriteLine($"  Plugin: {plugin.Name} ({plugin.FunctionCount} functions)");
    foreach (var func in plugin)
        Console.WriteLine($"    - {func.Name}: {func.Description}");
}

Console.WriteLine();
Console.WriteLine("Skills are ready for use with an AI provider.");
Console.WriteLine("Add .AddOpenAIChatCompletion() or similar to enable AI-powered skill invocation.");
