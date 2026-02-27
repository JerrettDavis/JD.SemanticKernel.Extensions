// Plugin Demo — loads a Claude Code plugin directory with skills and hooks.

using JD.SemanticKernel.Extensions;
using JD.SemanticKernel.Extensions.Hooks;
using Microsoft.SemanticKernel;

Console.WriteLine("=== SK Extensions — Plugin Demo ===");
Console.WriteLine();

// Create a kernel builder
var builder = Kernel.CreateBuilder();

// Set up a sample plugin directory structure
var pluginDir = Path.Combine(AppContext.BaseDirectory, "my-plugin");
var claudePluginDir = Path.Combine(pluginDir, ".claude-plugin");
var skillsDir = Path.Combine(pluginDir, "skills", "analyzer");
var hooksDir = Path.Combine(pluginDir, "hooks");

if (!Directory.Exists(claudePluginDir))
{
    Console.WriteLine("Creating sample plugin structure...");

    Directory.CreateDirectory(claudePluginDir);
    Directory.CreateDirectory(skillsDir);
    Directory.CreateDirectory(hooksDir);

    File.WriteAllText(Path.Combine(claudePluginDir, "plugin.json"), """
        {
            "name": "code-analyzer",
            "version": "1.0.0",
            "description": "Analyzes code for quality and security issues"
        }
        """);

    File.WriteAllText(Path.Combine(skillsDir, "SKILL.md"), """
        ---
        name: static-analyzer
        description: Runs static analysis on source code
        allowed-tools: [Read, Grep]
        ---
        # Static Analyzer

        Analyze the provided source code using static analysis techniques.
        Report findings categorized by severity: Critical, Warning, Info.

        Input: $ARGUMENTS
        """);

    File.WriteAllText(Path.Combine(hooksDir, "hooks.json"), """
        {
            "hooks": [
                {
                    "event": "PostToolUse",
                    "tool_name": "Write|Edit",
                    "command": "echo Post-edit hook triggered"
                }
            ]
        }
        """);
}

// Load the plugin
builder.AddClaudeCodePlugin(pluginDir);

// Add hooks
builder.AddClaudeCodeHooks(hooks =>
{
    hooks.OnFunctionInvoking(".*", ctx =>
    {
        Console.WriteLine($"  [Hook] Invoking: {ctx.Function.Name}");
        return Task.CompletedTask;
    });
});

var kernel = builder.Build();

Console.WriteLine($"Loaded {kernel.Plugins.Count} plugin(s):");
foreach (var plugin in kernel.Plugins)
{
    Console.WriteLine($"  Plugin: {plugin.Name} ({plugin.FunctionCount} functions)");
    foreach (var func in plugin)
        Console.WriteLine($"    - {func.Name}: {func.Description}");
}

Console.WriteLine();
Console.WriteLine("Plugin loaded with skills and hooks ready for AI-powered invocation.");
