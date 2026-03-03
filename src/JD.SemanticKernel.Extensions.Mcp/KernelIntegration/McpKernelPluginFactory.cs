using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JD.SemanticKernel.Extensions.Mcp.Transport;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Mcp.KernelIntegration;

/// <summary>
/// Factory for creating <see cref="KernelPlugin"/> instances from MCP server definitions.
/// </summary>
public static class McpKernelPluginFactory
{
    /// <summary>
    /// Creates a <see cref="KernelPlugin"/> that exposes all tools from the given MCP server.
    /// The server connection is established eagerly: <see cref="IMcpClient.InitializeAsync"/> and
    /// <see cref="IMcpClient.GetToolsAsync"/> are called during plugin creation. The returned plugin's
    /// functions share the underlying <see cref="IMcpClient"/> instance for all subsequent invocations.
    /// The caller is responsible for disposing the returned <see cref="IMcpClient"/> when the plugin
    /// is no longer needed (e.g., by registering it with <see cref="McpClientRegistry"/>).
    /// </summary>
    /// <param name="server">The MCP server definition to create a plugin from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A tuple of (<see cref="KernelPlugin"/>, <see cref="IMcpClient"/>) where the plugin contains
    /// one <see cref="KernelFunction"/> per MCP tool and the client must be disposed by the caller.
    /// </returns>
    public static async Task<(KernelPlugin Plugin, IMcpClient Client)> FromMcpServerAsync(
        McpServerDefinition server,
        CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(server);
#else
        if (server is null) throw new ArgumentNullException(nameof(server));
#endif

        var client = CreateClient(server);

        try
        {
            await client.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var tools = await client.GetToolsAsync(cancellationToken).ConfigureAwait(false);

            var functions = new List<KernelFunction>(tools.Count);
            foreach (var tool in tools)
                functions.Add(CreateKernelFunction(tool, server, client));

            var plugin = KernelPluginFactory.CreateFromFunctions(
                NormalizePluginName(server.Name),
                server.DisplayName,
                functions);

            return (plugin, client);
        }
        catch
        {
            // Dispose client only if we failed to create a plugin;
            // on success, the caller is responsible for disposal.
            if (client is IDisposable disposable)
                disposable.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Normalizes an MCP server or tool name to a valid Semantic Kernel plugin/function name
    /// (alphanumeric characters and underscores only).
    /// </summary>
    public static string NormalizePluginName(string name) => NormalizeName(name);

    private static IMcpClient CreateClient(McpServerDefinition server) =>
        server.Transport switch
        {
            McpTransportType.Stdio => StdioMcpClient.FromDefinition(server),
            McpTransportType.Http => HttpMcpClient.FromDefinition(server),
            _ => throw new NotSupportedException($"Transport '{server.Transport}' is not supported.")
        };

    private static KernelFunction CreateKernelFunction(
        McpToolDefinition tool,
        McpServerDefinition server,
        IMcpClient client)
    {
        var parameters = tool.Parameters.Select(p => new KernelParameterMetadata(p.Name)
        {
            Description = p.Description,
            IsRequired = p.IsRequired,
            ParameterType = MapJsonSchemaType(p.Type),
        }).ToList();

        return KernelFunctionFactory.CreateFromMethod(
            method: async (KernelArguments args, CancellationToken ct) =>
            {
                var arguments = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in tool.Parameters)
                {
                    if (args.TryGetValue(p.Name, out var val))
                        arguments[p.Name] = val;
                }

                var result = await client.InvokeAsync(tool.Name, arguments, ct).ConfigureAwait(false);

                if (result.IsError)
                    throw new KernelException(result.ErrorMessage ?? "MCP tool invocation failed.");

                return result.Content;
            },
            functionName: NormalizeName(tool.Name),
            description: tool.Description ?? $"Invokes the '{tool.Name}' tool from MCP server '{server.DisplayName}'.",
            parameters: parameters,
            returnParameter: new KernelReturnParameterMetadata { Description = "Tool output" });
    }

    /// <summary>
    /// Maps a JSON Schema <paramref name="schemaType"/> string to an appropriate CLR <see cref="Type"/>.
    /// </summary>
    private static Type MapJsonSchemaType(string? schemaType) =>
        schemaType switch
        {
            "integer" => typeof(long),
            "number" => typeof(double),
            "boolean" => typeof(bool),
            "array" => typeof(object),
            "object" => typeof(object),
            _ => typeof(string), // "string" and unknown types default to string
        };

    private static string NormalizeName(string name)
    {
        // MCP names may contain characters invalid for SK function names; normalize to alphanumeric + underscores.
        var chars = new char[name.Length];
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            chars[i] = char.IsLetterOrDigit(c) ? c : '_';
        }

        return new string(chars);
    }
}
