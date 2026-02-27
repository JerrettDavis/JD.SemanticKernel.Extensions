using System.Text.Json;
using JD.SemanticKernel.Extensions.Plugins;
using Xunit;

namespace JD.SemanticKernel.Extensions.Plugins.Tests;

public class PluginManifestTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var manifest = new PluginManifest();

        Assert.Equal(string.Empty, manifest.Name);
        Assert.Equal("0.0.0", manifest.Version);
        Assert.Equal(string.Empty, manifest.Description);
        Assert.Empty(manifest.Dependencies);
        Assert.Equal("skills", manifest.SkillsDir);
        Assert.Equal("hooks/hooks.json", manifest.HooksFile);
        Assert.Null(manifest.McpConfig);
        Assert.Null(manifest.ExtensionData);
    }

    [Fact]
    public void Deserialization_FromJson()
    {
        var json = """
            {
                "name": "my-plugin",
                "version": "2.1.0",
                "description": "A great plugin",
                "dependencies": ["dep-a", "dep-b"],
                "skills_dir": "custom-skills",
                "hooks_file": "custom/hooks.json",
                "mcp_config": "mcp.json"
            }
            """;

        var manifest = JsonSerializer.Deserialize<PluginManifest>(json);

        Assert.NotNull(manifest);
        Assert.Equal("my-plugin", manifest!.Name);
        Assert.Equal("2.1.0", manifest.Version);
        Assert.Equal("A great plugin", manifest.Description);
        Assert.Equal(2, manifest.Dependencies.Count);
        Assert.Contains("dep-a", manifest.Dependencies);
        Assert.Contains("dep-b", manifest.Dependencies);
        Assert.Equal("custom-skills", manifest.SkillsDir);
        Assert.Equal("custom/hooks.json", manifest.HooksFile);
        Assert.Equal("mcp.json", manifest.McpConfig);
    }

    [Fact]
    public void Deserialization_ExtraProperties_CapturedInExtensionData()
    {
        var json = """
            {
                "name": "ext-plugin",
                "custom_field": "custom_value"
            }
            """;

        var manifest = JsonSerializer.Deserialize<PluginManifest>(json);

        Assert.NotNull(manifest);
        Assert.NotNull(manifest!.ExtensionData);
        Assert.True(manifest.ExtensionData!.ContainsKey("custom_field"));
    }

    [Fact]
    public void Deserialization_MinimalJson_UsesDefaults()
    {
        var json = """{"name": "minimal"}""";

        var manifest = JsonSerializer.Deserialize<PluginManifest>(json);

        Assert.NotNull(manifest);
        Assert.Equal("minimal", manifest!.Name);
        Assert.Equal("0.0.0", manifest.Version);
        Assert.Equal("skills", manifest.SkillsDir);
    }

    [Fact]
    public void PropertyAssignment_Works()
    {
        var manifest = new PluginManifest
        {
            Name = "test",
            Version = "1.0.0",
            Description = "desc",
            SkillsDir = "s",
            HooksFile = "h/hooks.json",
            McpConfig = "m.json",
        };
        manifest.Dependencies.Add("dep1");

        Assert.Equal("test", manifest.Name);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.Single(manifest.Dependencies);
    }
}
