using System;
using System.Collections.Generic;
using System.Linq;

namespace JD.SemanticKernel.Extensions.Plugins;

/// <summary>
/// Resolves plugin dependencies using topological sort.
/// </summary>
public static class PluginDependencyResolver
{
    /// <summary>
    /// Orders plugins by their dependencies using topological sort.
    /// Plugins with no dependencies come first.
    /// </summary>
    /// <param name="plugins">The plugins to sort.</param>
    /// <returns>Plugins sorted in dependency order.</returns>
    /// <exception cref="InvalidOperationException">Thrown when circular dependencies are detected.</exception>
    public static IReadOnlyList<LoadedPlugin> Resolve(IEnumerable<LoadedPlugin> plugins)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(plugins);
#else
        if (plugins is null) throw new ArgumentNullException(nameof(plugins));
#endif

        var pluginList = plugins.ToList();
        var byName = pluginList.ToDictionary(
            p => p.Manifest.Name,
            StringComparer.OrdinalIgnoreCase);

        var sorted = new List<LoadedPlugin>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var plugin in pluginList)
            Visit(plugin.Manifest.Name, byName, visited, visiting, sorted);

        return sorted.AsReadOnly();
    }

    private static void Visit(
        string name,
        Dictionary<string, LoadedPlugin> byName,
        HashSet<string> visited,
        HashSet<string> visiting,
        List<LoadedPlugin> sorted)
    {
        if (visited.Contains(name))
            return;

        if (visiting.Contains(name))
            throw new InvalidOperationException(
                $"Circular plugin dependency detected involving '{name}'.");

        if (!byName.TryGetValue(name, out var plugin))
            return; // External dependency — skip

        visiting.Add(name);

        foreach (var dep in plugin.Manifest.Dependencies)
            Visit(dep, byName, visited, visiting, sorted);

        visiting.Remove(name);
        visited.Add(name);
        sorted.Add(plugin);
    }
}
