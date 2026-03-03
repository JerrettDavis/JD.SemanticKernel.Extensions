using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JD.SemanticKernel.Extensions.Mcp.Discovery;
using JD.SemanticKernel.Extensions.Mcp.KernelIntegration;
using JD.SemanticKernel.Extensions.Mcp.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Mcp;

/// <summary>
/// Extension methods for <see cref="IKernelBuilder"/> and <see cref="Kernel"/>
/// to register and use MCP servers as Semantic Kernel plugins.
/// </summary>
public static class KernelBuilderMcpExtensions
{
    /// <summary>
    /// Registers all six built-in MCP discovery providers and the <see cref="IMcpRegistry"/> in the DI container.
    /// </summary>
    /// <param name="builder">The kernel builder.</param>
    /// <param name="workingDirectory">Optional workspace root directory for file-based discovery providers.</param>
    /// <returns>The kernel builder for chaining.</returns>
    public static IKernelBuilder AddMcpDiscovery(
        this IKernelBuilder builder,
        string? workingDirectory = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(builder);
#else
        if (builder is null) throw new ArgumentNullException(nameof(builder));
#endif

        builder.Services.AddSingleton<IMcpDiscoveryProvider>(
            new ClaudeCodeMcpDiscoveryProvider(workingDirectory));
        builder.Services.AddSingleton<IMcpDiscoveryProvider>(
            new ClaudeDesktopMcpDiscoveryProvider());
        builder.Services.AddSingleton<IMcpDiscoveryProvider>(
            new VsCodeMcpDiscoveryProvider(workingDirectory));
        builder.Services.AddSingleton<IMcpDiscoveryProvider>(
            new CodexMcpDiscoveryProvider(workingDirectory));
        builder.Services.AddSingleton<IMcpDiscoveryProvider>(
            new CopilotMcpDiscoveryProvider());
        builder.Services.AddSingleton<IMcpDiscoveryProvider>(
            new JdCanonicalMcpDiscoveryProvider(workingDirectory));

        builder.Services.AddSingleton<IMcpRegistry>(sp =>
        {
            var providers = sp.GetServices<IMcpDiscoveryProvider>();
            return new McpRegistry(new List<IMcpDiscoveryProvider>(providers));
        });

        return builder;
    }

    /// <summary>
    /// Discovers all enabled MCP servers from registered providers and adds them as Semantic Kernel plugins.
    /// </summary>
    /// <param name="kernel">The kernel to add MCP plugins to.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static async Task AddMcpServersFromAllProvidersAsync(
        this Kernel kernel,
        CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(kernel);
#else
        if (kernel is null) throw new ArgumentNullException(nameof(kernel));
#endif

        var registry = kernel.Services.GetRequiredService<IMcpRegistry>();
        var servers = await registry.GetAllAsync(cancellationToken).ConfigureAwait(false);

        foreach (var server in servers)
        {
            if (!server.IsEnabled)
                continue;

            try
            {
                var plugin = await McpKernelPluginFactory
                    .FromMcpServerAsync(server, cancellationToken)
                    .ConfigureAwait(false);
                kernel.Plugins.Add(plugin);
            }
#pragma warning disable CA1031 // Individual server failures must not prevent other servers from loading
            catch (Exception)
#pragma warning restore CA1031
            {
                // Skip servers that fail to initialize
            }
        }
    }

    /// <summary>
    /// Adds a specific MCP server by name as a Semantic Kernel plugin.
    /// </summary>
    /// <param name="kernel">The kernel to add the MCP plugin to.</param>
    /// <param name="serverName">The name of the MCP server to add.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when the named server is not found.</exception>
    public static async Task AddMcpServerAsync(
        this Kernel kernel,
        string serverName,
        CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(kernel);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
#else
        if (kernel is null) throw new ArgumentNullException(nameof(kernel));
        if (string.IsNullOrWhiteSpace(serverName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(serverName));
#endif

        var registry = kernel.Services.GetRequiredService<IMcpRegistry>();
        var server = await registry.GetAsync(serverName, cancellationToken).ConfigureAwait(false);

        if (server is null)
            throw new InvalidOperationException($"MCP server '{serverName}' was not found in any registered discovery provider.");

        if (!server.IsEnabled)
            throw new InvalidOperationException($"MCP server '{serverName}' is disabled.");

        var plugin = await McpKernelPluginFactory
            .FromMcpServerAsync(server, cancellationToken)
            .ConfigureAwait(false);

        kernel.Plugins.Add(plugin);
    }
}
