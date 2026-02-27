using JD.SemanticKernel.Extensions.Hooks;
using JD.SemanticKernel.Extensions.Plugins;
using JD.SemanticKernel.Extensions.Skills;

namespace JD.SemanticKernel.Extensions.Plugins.Tests;

public class PluginDependencyResolverTests
{
    [Fact]
    public void Resolve_NoDependencies_ReturnsAll()
    {
        var plugins = new[]
        {
            CreatePlugin("a"),
            CreatePlugin("b"),
            CreatePlugin("c")
        };

        var sorted = PluginDependencyResolver.Resolve(plugins);

        Assert.Equal(3, sorted.Count);
    }

    [Fact]
    public void Resolve_LinearDependencies_OrdersCorrectly()
    {
        var a = CreatePlugin("a");
        var b = CreatePlugin("b", "a");
        var c = CreatePlugin("c", "b");

        var sorted = PluginDependencyResolver.Resolve(new[] { c, b, a });

        Assert.Equal("a", sorted[0].Manifest.Name);
        Assert.Equal("b", sorted[1].Manifest.Name);
        Assert.Equal("c", sorted[2].Manifest.Name);
    }

    [Fact]
    public void Resolve_CircularDependency_ThrowsInvalidOperation()
    {
        var a = CreatePlugin("a", "b");
        var b = CreatePlugin("b", "a");

        Assert.Throws<InvalidOperationException>(
            () => PluginDependencyResolver.Resolve(new[] { a, b }));
    }

    [Fact]
    public void Resolve_ExternalDependency_SkipsGracefully()
    {
        var a = CreatePlugin("a", "external-lib");

        var sorted = PluginDependencyResolver.Resolve(new[] { a });

        Assert.Single(sorted);
        Assert.Equal("a", sorted[0].Manifest.Name);
    }

    [Fact]
    public void Resolve_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => PluginDependencyResolver.Resolve(null!));
    }

    [Fact]
    public void Resolve_DiamondDependency_HandlesCorrectly()
    {
        var a = CreatePlugin("a");
        var b = CreatePlugin("b", "a");
        var c = CreatePlugin("c", "a");
        var d = CreatePlugin("d", "b", "c");

        var sorted = PluginDependencyResolver.Resolve(new[] { d, c, b, a });

        Assert.Equal("a", sorted[0].Manifest.Name);
        Assert.Equal(4, sorted.Count);
        // d must come after b and c
        var dIndex = sorted.ToList().FindIndex(p => p.Manifest.Name == "d");
        var bIndex = sorted.ToList().FindIndex(p => p.Manifest.Name == "b");
        var cIndex = sorted.ToList().FindIndex(p => p.Manifest.Name == "c");
        Assert.True(dIndex > bIndex);
        Assert.True(dIndex > cIndex);
    }

    private static LoadedPlugin CreatePlugin(string name, params string[] dependencies)
    {
        var manifest = new PluginManifest
        {
            Name = name,
            Version = "1.0.0"
        };

        foreach (var dep in dependencies)
            manifest.Dependencies.Add(dep);

        return new LoadedPlugin(
            manifest,
            Array.Empty<SkillDefinition>(),
            Array.Empty<HookDefinition>());
    }
}
