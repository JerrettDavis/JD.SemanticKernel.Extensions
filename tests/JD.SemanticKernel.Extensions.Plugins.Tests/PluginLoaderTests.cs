using JD.SemanticKernel.Extensions.Plugins;

namespace JD.SemanticKernel.Extensions.Plugins.Tests;

public class PluginLoaderTests
{
    [Fact]
    public void Load_WithManifest_ReturnsPlugin()
    {
        var tempDir = CreateTempPluginDirectory();
        try
        {
            var plugin = PluginLoader.Load(tempDir);

            Assert.Equal("test-plugin", plugin.Manifest.Name);
            Assert.Equal("1.0.0", plugin.Manifest.Version);
            Assert.Single(plugin.Skills);
            Assert.Equal("test-skill", plugin.Skills[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_WithoutManifest_UsesDirectoryName()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var skillsDir = Path.Combine(tempDir, "skills", "my-skill");
        Directory.CreateDirectory(skillsDir);
        File.WriteAllText(Path.Combine(skillsDir, "SKILL.md"), """
            ---
            name: my-skill
            description: Test
            ---
            Body.
            """);

        try
        {
            var plugin = PluginLoader.Load(tempDir);

            Assert.NotNull(plugin.Manifest);
            Assert.Single(plugin.Skills);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_WithHooksFile_ParsesHooks()
    {
        var tempDir = CreateTempPluginWithHooks();
        try
        {
            var plugin = PluginLoader.Load(tempDir);

            Assert.Single(plugin.Hooks);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Load_NonExistentDirectory_ThrowsDirectoryNotFound()
    {
        Assert.Throws<DirectoryNotFoundException>(
            () => PluginLoader.Load("/nonexistent/plugin"));
    }

    [Fact]
    public void LoadAll_MultiplePlugins_ReturnsAll()
    {
        var parentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(parentDir);

        // Plugin A
        var pluginA = Path.Combine(parentDir, "plugin-a");
        CreateMinimalPlugin(pluginA, "plugin-a");

        // Plugin B
        var pluginB = Path.Combine(parentDir, "plugin-b");
        CreateMinimalPlugin(pluginB, "plugin-b");

        try
        {
            var plugins = PluginLoader.LoadAll(parentDir);
            Assert.Equal(2, plugins.Count);
        }
        finally
        {
            Directory.Delete(parentDir, true);
        }
    }

    [Fact]
    public void ToKernelPlugin_CreatesKernelPlugin()
    {
        var tempDir = CreateTempPluginDirectory();
        try
        {
            var loaded = PluginLoader.Load(tempDir);
            var kernelPlugin = loaded.ToKernelPlugin();

            Assert.Equal("test_plugin", kernelPlugin.Name);
            Assert.Equal(1, kernelPlugin.FunctionCount);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static string CreateTempPluginDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var manifestDir = Path.Combine(tempDir, ".claude-plugin");
        var skillsDir = Path.Combine(tempDir, "skills", "test-skill");

        Directory.CreateDirectory(manifestDir);
        Directory.CreateDirectory(skillsDir);

        File.WriteAllText(Path.Combine(manifestDir, "plugin.json"), """
            {
                "name": "test-plugin",
                "version": "1.0.0",
                "description": "A test plugin"
            }
            """);

        File.WriteAllText(Path.Combine(skillsDir, "SKILL.md"), """
            ---
            name: test-skill
            description: A test skill
            ---
            # Test Skill

            Body content.
            """);

        return tempDir;
    }

    private static string CreateTempPluginWithHooks()
    {
        var tempDir = CreateTempPluginDirectory();
        var hooksDir = Path.Combine(tempDir, "hooks");
        Directory.CreateDirectory(hooksDir);

        File.WriteAllText(Path.Combine(hooksDir, "hooks.json"), """
            {
                "hooks": [
                    {
                        "event": "PostToolUse",
                        "tool_name": "Write",
                        "command": "echo done"
                    }
                ]
            }
            """);

        return tempDir;
    }

    private static void CreateMinimalPlugin(string dir, string name)
    {
        var manifestDir = Path.Combine(dir, ".claude-plugin");
        var skillsDir = Path.Combine(dir, "skills", "skill");
        Directory.CreateDirectory(manifestDir);
        Directory.CreateDirectory(skillsDir);

        File.WriteAllText(Path.Combine(manifestDir, "plugin.json"),
            $$"""{"name": "{{name}}", "version": "1.0.0"}""");
        File.WriteAllText(Path.Combine(skillsDir, "SKILL.md"), $"""
            ---
            name: {name}-skill
            description: Skill for {name}
            ---
            Body.
            """);
    }
}
