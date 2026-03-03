using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace JD.SemanticKernel.Extensions.Mcp.Discovery;

/// <summary>
/// Base class for file-based MCP discovery providers.
/// Handles JSON parsing and common error handling logic.
/// </summary>
public abstract class FileMcpDiscoveryProvider : IMcpDiscoveryProvider
{
    /// <inheritdoc/>
    public abstract string ProviderId { get; }

    /// <summary>
    /// Gets the list of file paths to search for MCP configuration, in priority order.
    /// </summary>
    protected abstract IEnumerable<string> GetConfigFilePaths();

    /// <summary>
    /// Parses MCP server definitions from the JSON content of a config file.
    /// </summary>
    /// <param name="json">The JSON content of the config file.</param>
    /// <param name="sourcePath">The path of the config file (for attribution).</param>
    protected abstract IReadOnlyList<McpServerDefinition> ParseConfig(string json, string sourcePath);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<McpServerDefinition>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new List<McpServerDefinition>();

        foreach (var path in GetConfigFilePaths())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(path))
                continue;

            try
            {
#if NET8_0_OR_GREATER
                var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
#else
                var json = File.ReadAllText(path);
#endif
                var servers = ParseConfig(json, path);
                results.AddRange(servers);
            }
#pragma warning disable CA1031 // Config file parsing must not crash the host process
            catch (Exception ex)
#pragma warning restore CA1031
            {
                if (ex is OperationCanceledException)
                    throw;
                // Ignore malformed or inaccessible config files
            }
        }

        return results;
    }
}
